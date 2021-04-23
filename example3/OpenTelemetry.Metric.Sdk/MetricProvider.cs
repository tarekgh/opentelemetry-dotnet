using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Metric;
using OpenTelemetry.Metric.Api;

namespace OpenTelemetry.Metric.Sdk
{
    public class MetricProvider
    {
        private List<(Predicate<MeterInstrument>, Action<InstrumentBuilder>)> instrumentConfigFuncs = new();
        private ConcurrentDictionary<MeterInstrument, InstrumentState> _instrumentStates = new ConcurrentDictionary<MeterInstrument, InstrumentState>();
        private ConcurrentDictionary<MeterInstrument, InstrumentState> _observableInstrumentStates = new ConcurrentDictionary<MeterInstrument, InstrumentState>();

        private string name;
        private int collectPeriod_ms = 2000;
        private bool isBuilt = false;
        private bool flushCollect = false;
        private CancellationTokenSource cts = new();

        private List<Exporter> exporters = new();

        private Task collectTask;
        private Task dequeueTask;

        // private MeterInstrumentListener listener;
        private MeterInstrumentListener<double> doubleListener   = new MeterInstrumentListener<double>() { MeasurementRecorded = (instrument, value, labels, cookie) => ((InstrumentState)cookie).Update(value, labels) };
        private MeterInstrumentListener<long>   longListener     = new MeterInstrumentListener<long>()   { MeasurementRecorded = (instrument, value, labels, cookie) => ((InstrumentState)cookie).Update((double)value, labels) };
        private MeterInstrumentListener<int>    intListener      = new MeterInstrumentListener<int>()    { MeasurementRecorded = (instrument, value, labels, cookie) => ((InstrumentState)cookie).Update((double)value, labels) };
        private MeterInstrumentListener<float>  floatListener    = new MeterInstrumentListener<float>()  { MeasurementRecorded = (instrument, value, labels, cookie) => ((InstrumentState)cookie).Update((double)value, labels) };
        private MeterInstrumentListener<short>  shortListener    = new MeterInstrumentListener<short>()  { MeasurementRecorded = (instrument, value, labels, cookie) => ((InstrumentState)cookie).Update((double)value, labels) };
        private MeterInstrumentListener<byte>   byteListener     = new MeterInstrumentListener<byte>()   { MeasurementRecorded = (instrument, value, labels, cookie) => ((InstrumentState)cookie).Update((double)value, labels) };

        private MeterListener meterListener;

        private ConcurrentQueue<Tuple<MeterInstrument, double, (string LabelName, string LabelValue)[], object>> incomingQueue = new();
        private bool useQueue = false;

        private void EnableListener(MeterInstrument instrument, object cookie)
        {
            if (instrument.IsObservable)
            {
                if (instrument is MeterObservableInstrument<long> || instrument is MeterObservableInstrument<double> || instrument is MeterObservableInstrument<short> ||
                    instrument is MeterObservableInstrument<int> || instrument is MeterObservableInstrument<byte> || instrument is MeterObservableInstrument<float>)
                {
                    _observableInstrumentStates.TryAdd(instrument, (InstrumentState)cookie);
                }
                return;
            }

            if (instrument is MeterInstrument<long> longInst)           { longInst.AddListener(longListener, cookie); return; }
            else if (instrument is MeterInstrument<double> doubleInst)  { doubleInst.AddListener(doubleListener, cookie); return; }
            else if (instrument is MeterInstrument<int> intInst)        { intInst.AddListener(intListener, cookie); return; }
            else if (instrument is MeterInstrument<float> floatInst)    { floatInst.AddListener(floatListener, cookie); return; }
            else if (instrument is MeterInstrument<short> shortInst)    { shortInst.AddListener(shortListener, cookie); return; }
            else if (instrument is MeterInstrument<byte> byteInst)      { byteInst.AddListener(byteListener, cookie); return; }
        }

        private void DisposeListeners()
        {
            doubleListener.Dispose();
            longListener.Dispose();
            intListener.Dispose();
            floatListener.Dispose();
            shortListener.Dispose();
            byteListener.Dispose();
        }

        //sealed class SdkInstrumentListener : MeterInstrumentListener
        //{
        //    MetricProvider _owner;
        //    public SdkInstrumentListener(MetricProvider owner) { _owner = owner; }
        //    protected override void MeasurementRecorded<T>(MeterInstrument instrument, T value, ReadOnlySpan<(string, string)> labels, object cookie)
        //    {
        //        InstrumentState state = (InstrumentState)cookie;
        //        state.Update(ToDouble(value), labels);
        //    }

        //    protected override void MeasurementRecorded(MeterInstrument instrument, double dValue, ReadOnlySpan<(string, string)> labels, object cookie)
        //    {
        //        InstrumentState state = (InstrumentState)cookie;
        //        state.Update(dValue, labels);
        //    }

        //    protected override void MeasurementRecorded(MeterInstrument instrument, long lValue, ReadOnlySpan<(string, string)> labels, object cookie)
        //    {
        //        InstrumentState state = (InstrumentState)cookie;
        //        state.Update((double)lValue, labels);
        //    }

        //    protected override void MeterInstrumentPublished(MeterInstrument instrument, MeterSubscribeOptions subscribeOptions)
        //    {
        //        InstrumentState state = _owner.GetInstrumentState(instrument);
        //        if (state != null)
        //        {
        //            subscribeOptions.Subscribe(state);
        //        }
        //    }

        //    protected override void MeterInstrumentUnpublished(MeterInstrument instrument, object cookie) =>
        //        _owner.RemoveInstrumentState(instrument, (InstrumentState)cookie);

        //    double ToDouble<T>(T value)
        //    {
        //        double dvalue = 0;
        //        if (value is double dval)
        //        {
        //            dvalue = dval;
        //        }
        //        else if(value is float fVal)
        //        {
        //            dvalue = fVal;
        //        }
        //        else if (value is long lval)
        //        {
        //            dvalue = lval;
        //        }
        //        else if (value is int ival)
        //        {
        //            dvalue = ival;
        //        }
        //        else if(value is short sVal)
        //        {
        //            dvalue = sVal;
        //        }
        //        else if(value is byte bVal)
        //        {
        //            dvalue = bVal;
        //        }

        //        return dvalue;
        //    }
        //}

        public MetricProvider()
        {
            // this.listener = new SdkInstrumentListener(this);
            meterListener = new MeterListener()
            {
                ShouldListenTo = (meter) => true,
                InstrumentEncountered = (instrument) =>
                {
                    InstrumentState state = GetInstrumentState(instrument);
                    if (state != null)
                    {
                        EnableListener(instrument, state);
                    }
                },
                InstrumentDisposed = (instrument) => RemoveInstrumentState(instrument)
            };
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

            if (exporters.Count == 0)
            {
                collectTask = Task.CompletedTask;
            }
            else
            {
                collectTask = Task.Run(async () =>
                {
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
                dequeueTask = Task.Run(async () =>
                {
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

            // listener.Start();
            Meter.AddListener(meterListener);
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

            flushCollect = true;
            collectTask.Wait();

            // listener.Dispose();
            DisposeListeners();

            foreach (var exporter in exporters)
            {
                exporter.BeginFlush();
            }

            foreach (var exporter in exporters)
            {
                exporter.Stop();
            }
        }

        void RemoveInstrumentState(MeterInstrument instrument)
        {
            if (instrument.IsObservable)
            {
                _observableInstrumentStates.TryRemove(instrument, out _);
            }
            else
            {
                _instrumentStates.TryRemove(instrument, out _);
            }
        }

        internal InstrumentState GetInstrumentState(MeterInstrument instrument)
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
                if (instrumentState != null)
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

        private void CollectState(MeterInstrument instrument, InstrumentState state, DateTimeOffset collectionTime, List<ExportItem> ret) =>
            state.Collect(instrument, (LabeledAggregationStatistics labeledAggStats) =>
            {
                var item = new ExportItem();
                item.dt = collectionTime;
                item.Labels = new MetricLabelSet(labeledAggStats.Labels);
                item.AggregationStatistics = labeledAggStats.AggregationStatistics;
                item.MeterName = instrument.Meter.Name;
                item.MeterVersion = instrument.Meter.Version;
                item.InstrumentName = instrument.Name;
                ret.Add(item);
            });

        private void CollectObservable<T>(MeterObservableInstrument<T> instrument, InstrumentState state, DateTimeOffset collectionTime, List<ExportItem> ret) where T : unmanaged
        {
            foreach (MeasurementObservaion<T> observedValue in instrument.Observe())
            {
                state.Update((double)(object)observedValue.Value, observedValue.Labels is null ? Array.Empty<(string, string)>().AsSpan() : observedValue.Labels.AsSpan());

                CollectState(instrument, state, collectionTime, ret);
            }
        }

        internal ExportItem[] Collect()
        {
            List<ExportItem> ret = new();

            if (isBuilt)
            {
                DateTimeOffset collectionTime = DateTimeOffset.UtcNow;
                // listener.RecordObservableMeters();

                foreach (KeyValuePair<MeterInstrument, InstrumentState> kv in _observableInstrumentStates)
                {
                    // We can assert here on the supported types (e.g. double, int, float,...etc.)
                    if (kv.Key is MeterObservableInstrument<long> longInstrument) CollectObservable<long>(longInstrument, kv.Value, collectionTime, ret);
                    else if (kv.Key is MeterObservableInstrument<double> doubleInstrument) CollectObservable<double>(doubleInstrument, kv.Value, collectionTime, ret);
                    else if (kv.Key is MeterObservableInstrument<int> intInstrument) CollectObservable<int>(intInstrument, kv.Value, collectionTime, ret);
                    else if (kv.Key is MeterObservableInstrument<short> shortInstrument) CollectObservable<short>(shortInstrument, kv.Value, collectionTime, ret);
                    else if (kv.Key is MeterObservableInstrument<float> floatInstrument) CollectObservable<float>(floatInstrument, kv.Value, collectionTime, ret);
                    else if (kv.Key is MeterObservableInstrument<byte> byteInstrument) CollectObservable<byte>(byteInstrument, kv.Value, collectionTime, ret);
                }

                foreach (KeyValuePair<MeterInstrument, InstrumentState> kv in _instrumentStates)
                {
                    CollectState(kv.Key, kv.Value, collectionTime, ret);
                }
            }

            return ret.ToArray();
        }
    }
}
