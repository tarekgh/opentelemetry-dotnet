using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using Microsoft.Diagnostics.Metric;
using OpenTelemetry.Metric.Api;
using System.Linq;

namespace OpenTelemetry.Metric.Sdk
{
    public class MetricProvider
    {
        private List<UnboundMeterInstrument> meters = new();

        private ConcurrentDictionary<MeterInstrument, InstrumentState> _instrumentStates = new ConcurrentDictionary<MeterInstrument, InstrumentState>();

        private string name;
        private int collectPeriod_ms = 2000;
        private bool isBuilt = false;
        private bool flushCollect = false;
        private CancellationTokenSource cts = new();

        private List<Tuple<string,string>> metricFilterList = new();

        private List<Exporter> exporters = new();

        private Task collectTask;
        private Task dequeueTask;

        private MeterInstrumentListener listener;

        private ConcurrentQueue<Tuple<MeterInstrument,double,(string LabelName,string LabelValue)[],object>> incomingQueue = new();
        private bool useQueue = false;

        public MetricProvider()
        {
            this.listener = new MeterInstrumentListener()
            {
                MeterInstrumentPublished = (instrument, options) => options.Subscribe(GetInstrumentState(instrument)),
                MeterInstrumentUnpublished = (instrument, cookie) => RemoveInstrumentState(instrument, (InstrumentState)cookie),
                MeasurementRecorded = OnMeasurementRecorded
            };
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

            if(exporters.Count == 0)
            {
                collectTask = Task.CompletedTask;
            }
            else
            {
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
            }


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

        void RemoveInstrumentState(MeterInstrument instrument, InstrumentState state)
        {
            _instrumentStates.TryRemove(KeyValuePair.Create(instrument, state));
        }

        InstrumentState GetInstrumentState(MeterInstrument instrument)
        {
            if (!_instrumentStates.TryGetValue(instrument, out InstrumentState instrumentState))
            {
                _instrumentStates.TryAdd(instrument, InstrumentState.Create(instrument));
                instrumentState = _instrumentStates[instrument];
            }
            return instrumentState;
        }

        public void OnMeasurementRecorded(MeterInstrument instrument, double value, ReadOnlySpan<(string LabelName, string LabelValue)> labels, object cookie)
        {
            if (useQueue)
            {
                // the label buffer may be stack-allocated so in order to queue we have to force
                // a heap allocation here
                incomingQueue.Enqueue(Tuple.Create(instrument, value, labels.ToArray(), cookie));
                return;
            }

            ProcessRecord(instrument, value, labels, cookie);
        }

        private void ProcessRecord(MeterInstrument instrument, double value, ReadOnlySpan<(string LabelName, string LabelValue)> labels, object cookie)
        {
            // TODO: we need to figure out our atomicity guarantees. Right now this function updates
            // potentially multiple aggregators for a single measurement and each aggregator might
            // have state spread across multiple fields. Other threads might be updating or reading
            // those values concurrently. At present this code is not thread-safe.

            if (isBuilt)
            {
                InstrumentState state = (InstrumentState)cookie;
                state.Update(value, labels);
            }
        }

        internal ExportItem[] Collect()
        {
            List<ExportItem> ret = new();

            if (isBuilt)
            {
                DateTimeOffset collectionTime = DateTimeOffset.UtcNow;
                listener.RecordObservableMeters();

                foreach (KeyValuePair<MeterInstrument, InstrumentState> kv in _instrumentStates)
                {
                    kv.Value.Collect(kv.Key, (LabeledAggregationStatistics labeledAggStats) =>
                    {
                        var item = new ExportItem();
                        item.dt = collectionTime;
                        item.Labels = new MetricLabelSet(labeledAggStats.Labels);
                        item.AggregationConfig = kv.Key.DefaultAggregation;
                        item.AggData = labeledAggStats.Statistics.ToArray();
                        item.MeterName = kv.Key.Meter.Name;
                        item.MeterVersion = kv.Key.Meter.Version;
                        item.InstrumentName = kv.Key.Name;
                        ret.Add(item);
                    });
                }
            }

            return ret.ToArray();
        }
    }
}
