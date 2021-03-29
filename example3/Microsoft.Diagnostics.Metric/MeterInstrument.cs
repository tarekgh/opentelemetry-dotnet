using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Metric
{
    public abstract class MeterInstrument
    {
        struct ListenerSubscription
        {
            public MeterInstrumentListener Listener;
            public object Cookie;
        }

        ListenerSubscription[] _subscriptions = Array.Empty<ListenerSubscription>();

        public abstract Meter Meter { get; }
        public abstract string Name { get; }
        public abstract Dictionary<string, string> StaticLabels { get; }
        public abstract AggregationConfiguration DefaultAggregation { get; }
        public bool Enabled => _subscriptions.Length > 0 || IsObservable;

        /// <summary>
        /// Adds the instrument to the list maintained on Meter which in turn
        /// makes it visible to listeners.
        /// </summary>
        protected void Publish()
        {
            Meter.PublishInstrument(this);
        }

        protected void RecordMeasurement(double val) =>
            RecordMeasurement(val, Array.Empty<(string LabelName, string LabelValue)>());

        protected void RecordMeasurement(double val, (string LabelName, string LabelValue) label)
        {
            ReadOnlySpan<(string LabelName, string LabelValue)> labels = MemoryMarshal.CreateReadOnlySpan(ref label, 1);
            RecordMeasurement(val, labels);
        }

        [StructLayout(LayoutKind.Sequential)]
        struct ThreeLabels
        {
            public (string LabelName, string LabelValue) Label1;
            public (string LabelName, string LabelValue) Label2;
            public (string LabelName, string LabelValue) Label3;
        }

        protected void RecordMeasurement(double val,
            (string LabelName, string LabelValue) label1,
            (string LabelName, string LabelValue) label2)
        {
            ThreeLabels threeLabels = new ThreeLabels();
            threeLabels.Label1 = label1;
            threeLabels.Label2 = label2;
            ReadOnlySpan<(string LabelName, string LabelValue)> labels = MemoryMarshal.CreateReadOnlySpan(ref threeLabels.Label1, 2);
            RecordMeasurement(val, labels);
        }

        protected void RecordMeasurement(double val,
            (string LabelName, string LabelValue) label1,
            (string LabelName, string LabelValue) label2,
            (string LabelName, string LabelValue) label3)
        {
            ThreeLabels threeLabels = new ThreeLabels();
            threeLabels.Label1 = label1;
            threeLabels.Label2 = label2;
            threeLabels.Label3 = label3;
            ReadOnlySpan<(string LabelName, string LabelValue)> labels = MemoryMarshal.CreateReadOnlySpan(ref threeLabels.Label1, 3);
            RecordMeasurement(val, labels);
        }

        protected void RecordMeasurement(double val, ReadOnlySpan<(string LabelName, string LabelValue)> labels)
        {
            // this captures a snapshot, _subscriptions array could be replaced while
            // we are invoking callbacks
            ListenerSubscription[] subscriptions = _subscriptions;
            for (int i = 0; i < subscriptions.Length; i++)
            {
                subscriptions[i].Listener.MeasurementRecorded?.Invoke(this, val, labels, subscriptions[i].Cookie);
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
            Debug.Assert(listener != null);

            // this should only be called under the metric collection lock
            ListenerSubscription[] subs = new ListenerSubscription[_subscriptions.Length + 1];
            Array.Copy(_subscriptions, subs, _subscriptions.Length);
            subs[_subscriptions.Length].Listener = listener;
            subs[_subscriptions.Length].Cookie = listenerCookie;
            _subscriptions = subs;
        }

        /// <summary>
        /// Returns true if the listener was previously subscribed
        /// </summary>
        /// <param name="listener"></param>
        /// <param name="cookie"></param>
        /// <returns></returns>
        internal bool RemoveSubscription(MeterInstrumentListener listener, out object cookie)
        {
            // only push metrics should have subscriptions
            Debug.Assert(!IsObservable);

            // this should only be called under metric collection lock
            for (int i = 0; i < _subscriptions.Length; i++)
            {
                if (_subscriptions[i].Listener == listener)
                {
                    cookie = _subscriptions[i].Cookie;
                    ListenerSubscription[] subs = new ListenerSubscription[_subscriptions.Length - 1];
                    Array.Copy(_subscriptions, subs, i);
                    Array.Copy(_subscriptions, i + 1, subs, i, _subscriptions.Length - i - 1);
                    _subscriptions = subs;
                    return true;
                }
            }
            cookie = null;
            return false;
        }
    }
}
