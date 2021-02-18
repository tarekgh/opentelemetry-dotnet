using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Threading;
using System.Text;
using System.Linq;
using Microsoft.Diagnostics.Metric;
using OpenTelmetry.Api;

namespace OpenTelmetry.Sdk
{
    public class SampleSdk
    {
        private readonly object lockMeters = new();
        private List<MeterBase> meters = new();

        private readonly object lockAggregateDict = new();
        private Dictionary<string, Aggregator> aggregateDict = new();

        private string name;
        private int collectPeriod_ms = 2000;
        private bool isBuilt = false;
        private CancellationTokenSource cancelTokenSrc = new();

        private List<Tuple<string,string>> metricFilterList = new();

        private List<(Type aggType, LabelSet[] labels)> aggregateByLabelSet = new();

        private List<Exporter> exporters = new();

        private Task collectTask;
        private Task dequeueTask;

        private SdkListener listener;

        private HashSet<MetricSource> sources = new();

        private ConcurrentQueue<Tuple<MeterBase,DateTimeOffset,object,LabelSet>> incomingQueue = new();
        private bool useQueue = false;

        public SampleSdk Name(string name)
        {
            this.name = name;
            this.listener = new SdkListener(this);

            return this;
        }

        public SampleSdk AttachSource(MetricSource source)
        {
            sources.Add(source);
            return this;
        }

        public SampleSdk AttachSource(string ns)
        {
            var source = MetricSource.GetSource(ns);
            sources.Add(source);
            return this;
        }

        public SampleSdk AddMetricInclusion(string term)
        {
            metricFilterList.Add(Tuple.Create("Include", term));
            return this;
        }

        public SampleSdk AddMetricExclusion(string term)
        {
            metricFilterList.Add(Tuple.Create("Exclude", term));
            return this;
        }

        public SampleSdk AggregateByLabels(Type aggType, params LabelSet[] labelset)
        {
            aggregateByLabelSet.Add((aggType, labelset));
            return this;
        }

        public SampleSdk AddExporter(Exporter exporter)
        {
            exporters.Add(exporter);
            return this;
        }

        public SampleSdk SetCollectionPeriod(int milliseconds)
        {
            collectPeriod_ms = milliseconds;
            return this;
        }

        public SampleSdk UseQueue()
        {
            useQueue = true;
            return this;
        }

        public SampleSdk Build()
        {
            // Start Periodic Collection Task

            var token = cancelTokenSrc.Token;

            collectTask = Task.Run(async () => {
                while (!token.IsCancellationRequested)
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

            foreach (var source in sources)
            {
                source.AttachListener(listener, "OTel SDK");
            }

            foreach (var exporter in exporters)
            {
                exporter.Start(token);
            }

            isBuilt = true;

            return this;
        }

        public void Stop()
        {
            cancelTokenSrc.Cancel();

            foreach (var source in sources)
            {
                source.DettachListener(listener, "OTel SDK");
            }

            collectTask.Wait();

            foreach (var exporter in exporters)
            {
                exporter.Stop();
            }
        }

        private List<Tuple<string,Type>> ExpandLabels(MeterBase meter, LabelSet labels)
        {
            var ns = meter.source.Name;
            var name = meter.MetricName;
            var type = meter.MetricType;

            // TODO: Area for performance improvements

            // TODO: Find a more performant way to avoid string interpolation.  Maybe class for segmented string list.  Reuse Labelset?

            var qualifiedName = ($"{ns}/{type}/{name}");

            // Merge Bound and Ad-Hoc labels into one

            Dictionary<string,string> labelDict = new();

            var boundLabels = meter.Labels.GetLabels();
            foreach (var label in boundLabels)
            {
                labelDict[label.name] = label.value;
            }

            var adhocLabels = labels.GetLabels();
            foreach (var label in adhocLabels)
            {
                labelDict[label.name] = label.value;
            }

            // Get Hints

            Dictionary<string,string> hints = new();

            var hintLabels = meter.Hints.GetLabels();
            foreach (var label in hintLabels)
            {
                hints[label.name] = label.value;
            }

            // Determine how to expand into different aggregates instances

            List<Tuple<string,Type>> label_aggregates = new();

            // TODO: Use Meter.Hints to determine how to expand labels...
            var defaultAggType = hints.GetValueOrDefault("DefaultAggregator", "Sum");
            Type defaultAgg = 
                defaultAggType == "Sum" ? typeof(CountSumMinMax)
                : defaultAggType == "Histogram" ? typeof(LabelHistogram)
                : typeof(CountSumMinMax);

            // Meter for total (dropping all labels)
            label_aggregates.Add(Tuple.Create($"{qualifiedName}/{defaultAgg.Name}/_Total", defaultAgg));

            // Meter for each configured dimension
            foreach (var aggSet in aggregateByLabelSet)
            {
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
                        var dim = String.Join("/", paths);
                        label_aggregates.Add(Tuple.Create($"{qualifiedName}/{aggSet.aggType.Name}/{dim}", aggSet.aggType));
                    }
                }

                if (aggSet.labels.Length == 0)
                {
                    label_aggregates.Add(Tuple.Create($"{qualifiedName}/{aggSet.aggType.Name}/_Total", aggSet.aggType));
                }
            }

            // Apply inclusion/exclusion filters
            foreach (var filter in metricFilterList)
            {
                // TODO: Need to optimize!

                if (filter.Item1 == "Include")
                {
                    label_aggregates = label_aggregates.Where((k) => k.Item1.Contains(filter.Item2)).ToList();
                }
                else
                {
                    label_aggregates = label_aggregates.Where((k) => !k.Item1.Contains(filter.Item2)).ToList();
                }
            }

            return label_aggregates;
        }

        public bool OnRecord<T>(MeterBase meter, DateTimeOffset dt, T value, LabelSet labels)
        {
            if (useQueue)
            {
                incomingQueue.Enqueue(Tuple.Create(meter, dt, (object) value, labels));
                return true;
            }

            return ProcessRecord<T>(meter, dt, value, labels);
        }

        private bool ProcessRecord<T>(MeterBase meter, DateTimeOffset dt, T value, LabelSet labels)
        {
            if (isBuilt && meter.Enabled)
            {
                // Expand out all the aggregates we need to update based on this measurement
                var label_aggregates = ExpandLabels(meter, labels);

                lock (lockAggregateDict)
                {
                    foreach (var tup in label_aggregates)
                    {
                        var key = tup.Item1;
                        var type = tup.Item2;

                        Aggregator aggdata;
                        if (!aggregateDict.TryGetValue(key, out aggdata))
                        {
                            aggdata = (Aggregator) Activator.CreateInstance(type);
                            aggregateDict[key] = aggdata;
                        }

                        aggdata.Update(meter, value, labels);
                    }
                }

                return true;
            }

            return false;
        }

        private ExportItem[] Collect()
        {
            List<ExportItem> ret = new();

            Console.WriteLine($"*** Collect {name}...");

            if (isBuilt)
            {
                List<string> exports = new();

                // Reset all aggregates!
                var oldAggDict = Interlocked.Exchange(ref aggregateDict, new Dictionary<string, Aggregator>());

                foreach (var kv in oldAggDict)
                {
                    StringBuilder sb = new StringBuilder();

                    sb.AppendLine($"{name}: {kv.Key}");

                    sb.Append($"    {kv.Value.GetType().Name}: ");

                    // TODO: Print out each specific type of Aggregation.
                    if (kv.Value is CountSumMinMax cnt)
                    {
                        sb.AppendLine($"n={cnt.count}, sum={cnt.sum}, min={cnt.min}, max={cnt.max}");
                    }
                    else if (kv.Value is LabelHistogram hgm)
                    {
                        var details = String.Join(", ", hgm.bins.Select(x => $"{x.Key}={x.Value}"));
                        sb.AppendLine(details);
                    }
                    else
                    {
                        sb.AppendLine("-");
                    }

                    exports.Add(sb.ToString());
                }

                exports.Sort();
                foreach (var item in exports)
                {
                    ret.Add(new StringExportItem(item));
                }
            }

            return ret.ToArray();
        }
   }
}