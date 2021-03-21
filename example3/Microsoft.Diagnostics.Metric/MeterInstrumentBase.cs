using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Metric
{
    public abstract class MeterInstrumentBase
    {
        struct ListenerSubscription
        {
            public MeterInstrumentListener Listener;
            public object Cookie;
        }

        ListenerSubscription[] _subscriptions = Array.Empty<ListenerSubscription>();

        public abstract Meter Meter { get; }
        public abstract string Name { get; }
        public abstract string[] LabelNames { get; }
        public abstract Dictionary<string, string> StaticLabels { get; }
        public abstract AggregationConfiguration DefaultAggregation { get; }
        public bool Enabled => _subscriptions.Length > 0 || IsObservable;

        protected void RecordMeasurement(double val, string[] labelValues)
        {
            // this captures a snapshot, _subscriptions array could be replaced while
            // we are invoking callbacks
            ListenerSubscription[] subscriptions = _subscriptions;
            for (int i = 0; i < subscriptions.Length; i++)
            {
                subscriptions[i].Listener.MeasurementRecorded(this, val, labelValues, subscriptions[i].Cookie);
            }
        }

        protected internal virtual bool IsObservable => false;

        // This is used by observable metrics to pull a measurement from the meter.
        protected internal virtual void Observe(MeasurementObserver observer)
        {
            // Observable metrics can override this and invoke the callback one or more times
            throw new InvalidOperationException("This meter is not observable");
        }

        internal void AddSubscription(MeterInstrumentListener listener, object listenerCookie)
        {
            // only push metrics should have subscriptions
            Debug.Assert(!IsObservable);

            // this should only be called under the metric collection lock
            ListenerSubscription[] subs = new ListenerSubscription[_subscriptions.Length + 1];
            Array.Copy(_subscriptions, subs, _subscriptions.Length);
            subs[_subscriptions.Length].Listener = listener;
            subs[_subscriptions.Length].Cookie = listenerCookie;
            _subscriptions = subs;
        }

        internal object RemoveSubscription(MeterInstrumentListener listener)
        {
            // only push metrics should have subscriptions
            Debug.Assert(!IsObservable);

            // this should only be called under metric collection lock
            for (int i = 0; i < _subscriptions.Length; i++)
            {
                if (_subscriptions[i].Listener == listener)
                {
                    object cookie = _subscriptions[i].Cookie;
                    ListenerSubscription[] subs = new ListenerSubscription[_subscriptions.Length - 1];
                    Array.Copy(_subscriptions, subs, i);
                    Array.Copy(_subscriptions, i + 1, subs, i, _subscriptions.Length - i - 1);
                    _subscriptions = subs;
                    return cookie;
                }
            }
            return null;
        }
    }
}
