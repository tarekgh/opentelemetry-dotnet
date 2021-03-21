using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Metric
{

    public class MeterInstrumentListener
    {
        // this dictionary is synchronized by the MetricCollection.s_lock
        Dictionary<MeterInstrumentBase, object> _subscribedObservableMeters = new Dictionary<MeterInstrumentBase, object>();

        public Action<MeterInstrumentBase, MeterSubscribeOptions> MeterInstrumentPublished;
        public Action<MeterInstrumentBase, double, string[], object> MeasurementRecorded;
        public Action<MeterInstrumentBase, object> MeterInstrumentUnpublished;

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
                Dictionary<MeterInstrumentBase, object> subscriptionCopy = new(_subscribedObservableMeters);
            }
            MeasurementObserver observer = new MeasurementObserver(this);
            foreach (KeyValuePair<MeterInstrumentBase, object> kv in _subscribedObservableMeters)
            {
                observer.CurrentMeter = kv.Key;
                observer.CurrentCookie = kv.Value;
                observer.CurrentMeter.Observe(observer);
            }
        }

        public void Dispose()
        {
            MeterInstrumentCollection.Instance.RemoveListener(this);
        }

        internal void SubscribeObservableMeter(MeterInstrumentBase meter, object listenerCookie)
        {
            _subscribedObservableMeters[meter] = listenerCookie;
        }

        internal object UnsubscribeObservableMeter(MeterInstrumentBase meter)
        {
            _subscribedObservableMeters.Remove(meter, out object cookie);
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
        internal MeterInstrumentBase CurrentMeter { get; set; }
        internal object CurrentCookie { get; set; }

        public void Observe(double value)
        {
            Listener.MeasurementRecorded(CurrentMeter, value, Array.Empty<string>(), CurrentCookie);
        }

        public void Observe(double value, params string[] labelValues)
        {
            Listener.MeasurementRecorded(CurrentMeter, value, labelValues, CurrentCookie);
        }
    }

}
