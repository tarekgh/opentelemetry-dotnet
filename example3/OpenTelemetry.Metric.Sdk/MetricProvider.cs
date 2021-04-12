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
        private List<(Predicate<MeterInstrument>, Action<InstrumentBuilder>)> instrumentConfigFuncs = new();
        private ConcurrentDictionary<MeterInstrument, InstrumentState> _instrumentStates = new ConcurrentDictionary<MeterInstrument, InstrumentState>();

        private string name;
        private int collectPeriod_ms = 2000;
        private bool isBuilt = false;
        private bool flushCollect = false;
        private CancellationTokenSource cts = new();

        private List<Exporter> exporters = new();

        private Task collectTask;
        private Task dequeueTask;

        private MeterInstrumentListener listener;

        private ConcurrentQueue<Tuple<MeterInstrument,double,(string LabelName,string LabelValue)[],object>> incomingQueue = new();
        private bool useQueue = false;

        sealed class SdkInstrumentListener : MeterInstrumentListener
        {
            MetricProvider _owner;
            public SdkInstrumentListener(MetricProvider owner) { _owner = owner; }
            protected override void MeasurementRecorded(MeterInstrument instrument, double value, ReadOnlySpan<(string, string)> labels, object cookie)
            {
                InstrumentState state = (InstrumentState)cookie;
                state.Update(value, labels);
            }

            protected override void MeterInstrumentPublished(MeterInstrument instrument, MeterSubscribeOptions subscribeOptions)
            {
                InstrumentState state = _owner.GetInstrumentState(instrument);
                if(state != null)
                {
                    subscribeOptions.Subscribe(state);
                }
            }

            protected override void MeterInstrumentUnpublished(MeterInstrument instrument, object cookie) =>
                _owner.RemoveInstrumentState(instrument, (InstrumentState)cookie);
        }

        public MetricProvider()
        {
            this.listener = new SdkInstrumentListener(this);
        }

        public MetricProvider Name(string name)
        {
            this.name = name;
            return this;
        }

        public MetricProvider Include(string term)
        {
            Include(i => i.Meter.Name == term, config => { });
            return this;
        }

        public MetricProvider Include(string term, Action<InstrumentBuilder> configFunc)
        {
            Include(i => i.Meter.Name == term, configFunc);
            return this;
        }

        public MetricProvider Include(Predicate<MeterInstrument> instrumentFilter, Action<InstrumentBuilder> configFunc)
        {
            instrumentConfigFuncs.Add((instrumentFilter, configFunc));
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
                InstrumentBuilder instrumentBuilder = null;
                foreach (var (filter, configFunc) in instrumentConfigFuncs)
                {
                    if (filter(instrument))
                    {
                        instrumentBuilder ??= new InstrumentBuilder(instrument);
                        configFunc(instrumentBuilder);
                    }
                }
                instrumentState = instrumentBuilder?.Build();
                if(instrumentState != null)
                {
                    _instrumentStates.TryAdd(instrument, instrumentState);
                    instrumentState = _instrumentStates[instrument];
                }
            }
            return instrumentState;
        }

        private void ProcessRecord(MeterInstrument instrument, double value, ReadOnlySpan<(string LabelName, string LabelValue)> labels, object cookie)
        {

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
                        item.AggregationStatistics = labeledAggStats.AggregationStatistics;
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
