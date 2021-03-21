using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using Microsoft.Diagnostics.Metric;
using OpenTelemetry.Metric.Api;

namespace OpenTelemetry.Metric.Sdk
{
    public class MetricProvider
    {
        private readonly object lockMeters = new();
        private List<MeterInstrument> meters = new();

        private ConcurrentDictionary<AggregatorKey, AggregatorState> aggregateDict = new();
        private Action<MeterInstrumentBase, double, AggregatorKey> _updateAggregatorByKeyFunc;

        private string name;
        private int collectPeriod_ms = 2000;
        private bool isBuilt = false;
        private bool flushCollect = false;
        private CancellationTokenSource cts = new();

        private List<Tuple<string,string>> metricFilterList = new();

        private List<(Aggregator agg, MetricLabelSet[] labels)> aggregateByLabelSet = new();

        private List<Exporter> exporters = new();

        private Task collectTask;
        private Task dequeueTask;

        private MeterInstrumentListener listener;

        private ConcurrentQueue<Tuple<MeterInstrumentBase,double,string[],object>> incomingQueue = new();
        private bool useQueue = false;

        public MetricProvider()
        {
            this.listener = new MeterInstrumentListener()
            {
                MeterInstrumentPublished = OnMeterPublished,
                MeasurementRecorded = OnMeasurementRecorded
            };
            // cache this delegate so we don't keep allocating it
            this._updateAggregatorByKeyFunc = this.UpdateAggregatorByKey;
        }

        public MetricProvider Name(string name)
        {
            this.name = name;
            return this;
        }

        public MetricProvider Include(string term)
        {
            metricFilterList.Add(Tuple.Create("Include", term));
            return this;
        }

        public MetricProvider AddMetricExclusion(string term)
        {
            metricFilterList.Add(Tuple.Create("Exclude", term));
            return this;
        }

        public MetricProvider AggregateByLabels(Aggregator agg, params MetricLabelSet[] labelset)
        {
            aggregateByLabelSet.Add((agg, labelset));
            return this;
        }

        public MetricProvider AddExporter(Exporter exporter)
        {
            exporters.Add(exporter);
            return this;
        }

        public MetricProvider SetCollectionPeriod(int milliseconds)
        {
            collectPeriod_ms = milliseconds;
            return this;
        }

        public MetricProvider UseQueue()
        {
            useQueue = true;
            return this;
        }

        public MetricProvider Build()
        {
            // Start Periodic Collection Task

            var token = cts.Token;

            collectTask = Task.Run(async () => {
                while (!flushCollect && !token.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(this.collectPeriod_ms, token);
                    }
                    catch (TaskCanceledException)
                    {
                        // Do Nothing
                    }

                    var export = Collect();

                    foreach (var exporter in exporters)
                    {
                        exporter.Export(export);
                    }
                }
            });

            if (useQueue)
            {
                dequeueTask = Task.Run(async () => {
                    while (!token.IsCancellationRequested)
                    {
                        if (incomingQueue.TryDequeue(out var record))
                        {
                            ProcessRecord(record.Item1, record.Item2, record.Item3, record.Item4);
                        }
                        else
                        {
                            try
                            {
                                await Task.Delay(100, token);
                            }
                            catch (TaskCanceledException)
                            {
                                // Do Nothing
                            }
                        }
                    }
                });
            }

            foreach (var exporter in exporters)
            {
                exporter.Start(token);
            }

            listener.Start();
            isBuilt = true;

            return this;
        }

        public void Stop()
        {
            Stop(TimeSpan.Zero);
        }

        public void Stop(TimeSpan timeout)
        {
            cts.CancelAfter(timeout);

            listener.Dispose();

            flushCollect = true;
            collectTask.Wait();

            foreach (var exporter in exporters)
            {
                exporter.BeginFlush();
            }

            foreach (var exporter in exporters)
            {
                exporter.Stop();
            }
        }

        private void VisitAggregatorKeys(MeterInstrumentBase instrument, double value, string[] labelValues, Action<MeterInstrumentBase, double, AggregatorKey> visitor)
        {
            // Determine how to expand into different aggregates instances

            // The SDK can use any logic of configuration it wants to determine that actual
            // aggregation mechanism to use. This is a trivial implementation that uses
            // whatever the library suggested to use.
            AggregationConfiguration aggConfig = instrument.DefaultAggregation;

            // Aggregate for total (dropping all labels)
            visitor(instrument, value, new AggregatorKey(instrument.Meter, instrument.Name, aggConfig, MetricLabelSet.DefaultLabelSet));

            // Aggregate for identity (preserving all labels)
            visitor(instrument, value, new AggregatorKey(instrument.Meter, instrument.Name, aggConfig, new MetricLabelSet(instrument.LabelNames, labelValues)));

            /*
            // Aggregate for each configured dimension
            foreach (var aggSet in aggregateByLabelSet)
            {
                var aggName = aggSet.agg.GetType().Name;
                var agg = aggSet.agg;

                foreach (var ls in aggSet.labels)
                {
                    List<string> paths = new();

                    foreach (var kv in ls.GetLabels())
                    {
                        var lskey = kv.Item1;
                        var lsval = kv.Item2;

                        if (labelDict.TryGetValue(lskey, out var val))
                        {
                            if (lsval == "*")
                            {
                                paths.Add($"{lskey}={val}");
                            }
                            else
                            {
                                var itemval = lsval.Split(",");
                                if (itemval.Contains(val))
                                {
                                    paths.Add($"{lskey}={val}");
                                }
                            }
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (paths.Count() > 0)
                    {
                        paths.Sort();
                        var dimLabels = new MetricLabelSet(paths.Select(k => {
                            var kv = k.Split("=");
                            return (kv[0], kv[1]);
                        }).ToArray());
                        label_aggregates.Add((new AggregatorKey(name, meter.MetricType, aggName, dimLabels), agg));
                    }
                }

                if (aggSet.labels.Length == 0)
                {
                    label_aggregates.Add((new AggregatorKey(name, meter.MetricType, aggName, MetricLabelSet.DefaultLabelSet), agg));
                }
            }

            // Apply inclusion/exclusion filters
            foreach (var filter in metricFilterList)
            {
                // TODO: Need to optimize!

                if (filter.Item1 == "Include")
                {
                    label_aggregates = label_aggregates.Where((k) => k.aggKey.name.Contains(filter.Item2)).ToList();
                }
                else
                {
                    label_aggregates = label_aggregates.Where((k) => !k.aggKey.name.Contains(filter.Item2)).ToList();
                }
            }

            return label_aggregates;
            */
        }

        public void OnMeterPublished(MeterInstrumentBase meter, MeterSubscribeOptions options)
        {
            if(meter is LabeledMeterInstrument)
            {
                options.Subscribe(GetLabeledMeterCookie((LabeledMeterInstrument)meter));
            }
            else
            {
                options.Subscribe();
            }
        }

        object GetLabeledMeterCookie(LabeledMeterInstrument meter)
        {
            List<AggregatorKey> aggKeys = new List<AggregatorKey>();
            VisitAggregatorKeys(meter, 0, meter.LabelValues, (_, _, key) => aggKeys.Add(key));
            if (aggKeys.Count == 1)
            {
                return aggKeys[0];
            }
            else
            {
                return aggKeys.ToArray();
            }
        }

        public void OnMeasurementRecorded(MeterInstrumentBase meter, double value, string[] labelValues, object cookie)
        {
            if (useQueue)
            {
                incomingQueue.Enqueue(Tuple.Create(meter, value, labelValues, cookie));
                return;
            }

            ProcessRecord(meter, value, labelValues, cookie);
        }

        private void ProcessRecord(MeterInstrumentBase meter, double value, string[] labelValues, object cookie)
        {
            // TODO: we need to figure out our atomicity guarantees. Right now this function updates
            // potentially multiple aggregators for a single measurement and each aggregator might
            // have state spread across multiple fields. Other threads might be updating or reading
            // those values concurrently. At present this code is not thread-safe.

            if (isBuilt)
            {
                // PERF: if we didn't wipe out the aggregateDict on every collect cycle the cookie
                // could cache the AggregationState items directly rather than the keys. This would
                // probably save us some CPU cycles in saved Dictionary lookup costs
                if (cookie is AggregatorKey)
                {
                    UpdateAggregatorByKey(meter, value, (AggregatorKey)cookie);
                }
                else if (cookie is AggregatorKey[])
                {
                    foreach(AggregatorKey key in (AggregatorKey[])cookie)
                    {
                        UpdateAggregatorByKey(meter, value, key);
                    }
                }
                else
                {
                    // we haven't cached the aggregators so we need to calculate them on the fly
                    VisitAggregatorKeys(meter, value, labelValues, _updateAggregatorByKeyFunc);
                }
            }
        }

        void UpdateAggregatorByKey(MeterInstrumentBase meter, double value, AggregatorKey aggKey)
        {
            AggregatorState aggState;
            if (!aggregateDict.TryGetValue(aggKey, out aggState))
            {
                aggState = CreateAggregatorState(aggKey.AggregationConfig);
                aggregateDict[aggKey] = aggState;
            }

            aggState.Update(meter, value);
        }

        AggregatorState CreateAggregatorState(AggregationConfiguration config)
        {
            if(config is SumAggregation)
            {
                return new SumCountMinMaxState();
            }
            else if(config is LastValueAggregation)
            {
                return new LastValueState();
            }
            else 
            {
                // for any unsupported aggregations this SDK converts it to SumCountMinMax
                // this is a flexible policy we can make it do whatever we want
                return new SumCountMinMaxState();
            }
        }

        private ExportItem[] Collect()
        {
            List<ExportItem> ret = new();

            Console.WriteLine($"*** Collect {name}...");

            if (isBuilt)
            {
                listener.RecordObservableMeters();

                // Reset all aggregate states!
                var oldAggStates = Interlocked.Exchange(ref aggregateDict, new ConcurrentDictionary<AggregatorKey, AggregatorState>());

                foreach (var kv in oldAggStates)
                {
                    var item = new ExportItem();
                    item.dt = DateTimeOffset.UtcNow;
                    item.Labels = kv.Key.labels;
                    item.AggregationConfig = kv.Key.AggregationConfig;
                    item.AggData = kv.Value.Serialize();
                    item.LibName = kv.Key.meter.Name;
                    item.LibVersion = kv.Key.meter.Version;
                    item.MeterName = kv.Key.name;
                    ret.Add(item);
                }
            }

            return ret.ToArray();
        }
    }
}
