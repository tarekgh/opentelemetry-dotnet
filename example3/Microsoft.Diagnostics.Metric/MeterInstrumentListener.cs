using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Metric
{

    public delegate void MeasurementRecorded(MeterInstrument instrument, double value, ReadOnlySpan<ValueTuple<string, string>> labels, object cookie);

    public class MeterInstrumentListener
    {
        // this dictionary is synchronized by the MetricCollection.s_lock
        Dictionary<MeterInstrument, object> _subscribedObservableMeters = new Dictionary<MeterInstrument, object>();

        public Action<MeterInstrument, MeterSubscribeOptions> MeterInstrumentPublished;
        public MeasurementRecorded MeasurementRecorded;
        public Action<MeterInstrument, object> MeterInstrumentUnpublished;

        public void Start()
        {
            MeterInstrumentCollection.Instance.AddListener(this);
        }

        public void RecordObservableMeters()
        {
            // This ensures that meters can't be published/unpublished while we are trying to traverse the
            // list. The Observe callback could still be concurrent with Dispose().
            lock (MeterInstrumentCollection.Lock)
            {
                Dictionary<MeterInstrument, object> subscriptionCopy = new(_subscribedObservableMeters);
            }
            MeasurementObserver observer = new MeasurementObserver(this);
            foreach (KeyValuePair<MeterInstrument, object> kv in _subscribedObservableMeters)
            {
                observer.CurrentInstrument = kv.Key;
                observer.CurrentCookie = kv.Value;
                observer.CurrentInstrument.Observe(observer);
            }
        }

        public void Dispose()
        {
            MeterInstrumentCollection.Instance.RemoveListener(this);
        }

        internal void SubscribeObservableInstrument(MeterInstrument instrument, object listenerCookie)
        {
            _subscribedObservableMeters[instrument] = listenerCookie;
        }

        internal object UnsubscribeObservableInstrument(MeterInstrument instrument)
        {
            _subscribedObservableMeters.Remove(instrument, out object cookie);
            return cookie;
        }
    }

    public class MeasurementObserver
    {
        internal MeasurementObserver(MeterInstrumentListener listener)
        {
            Listener = listener;
        }
        internal MeterInstrumentListener Listener { get; private set; }
        internal MeterInstrument CurrentInstrument { get; set; }
        internal object CurrentCookie { get; set; }

        public void Observe(double value)
        {
            Listener.MeasurementRecorded(CurrentInstrument, value, Array.Empty<ValueTuple<string,string>>().AsSpan(), CurrentCookie);
        }

        public void Observe(double value, ValueTuple<string, string> label)
        {
            ReadOnlySpan<(string LabelName, string LabelValue)> labels = MemoryMarshal.CreateReadOnlySpan(ref label, 1);
            Listener.MeasurementRecorded(CurrentInstrument, value, labels, CurrentCookie);
        }

        public void Observe(double value, ReadOnlySpan<ValueTuple<string,string>> labels)
        {
            Listener.MeasurementRecorded(CurrentInstrument, value, labels, CurrentCookie);
        }
    }

}
