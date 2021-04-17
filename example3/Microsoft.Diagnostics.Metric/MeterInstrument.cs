using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Metric
{
    public abstract class MeterInstrument
    {
        protected struct ListenerSubscription
        {
            public MeterInstrumentListener Listener;
            public object Cookie;
        }

        [StructLayout(LayoutKind.Sequential)]
        protected struct TwoLabels
        {
            public (string LabelName, string LabelValue) Label1;
            public (string LabelName, string LabelValue) Label2;
        }

        [StructLayout(LayoutKind.Sequential)]
        protected struct ThreeLabels
        {
            public (string LabelName, string LabelValue) Label1;
            public (string LabelName, string LabelValue) Label2;
            public (string LabelName, string LabelValue) Label3;
        }

        protected ListenerSubscription[] _subscriptions = Array.Empty<ListenerSubscription>();

        public abstract Meter Meter { get; }
        public abstract string Name { get; }
        public bool Enabled => _subscriptions.Length > 0 || IsObservable;

        /// <summary>
        /// Adds the instrument to the list maintained on Meter which in turn
        /// makes it visible to listeners.
        /// </summary>
        protected void Publish()
        {
            Meter.PublishInstrument(this);
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

    public abstract class MeterInstrument<T> : MeterInstrument where T : unmanaged
    {
        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void RecordMeasurement(T val,
            (string LabelName, string LabelValue) label1,
            (string LabelName, string LabelValue) label2)
        {
            TwoLabels twoLabels = new TwoLabels();
            twoLabels.Label1 = label1;
            twoLabels.Label2 = label2;
            ReadOnlySpan<(string LabelName, string LabelValue)> labels = MemoryMarshal.CreateReadOnlySpan(ref twoLabels.Label1, 2);
            RecordMeasurement(val, labels);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void RecordMeasurement(T val) =>
            RecordMeasurement(val, Array.Empty<(string LabelName, string LabelValue)>());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void RecordMeasurement(T val, (string LabelName, string LabelValue) label)
        {
            ReadOnlySpan<(string LabelName, string LabelValue)> labels = MemoryMarshal.CreateReadOnlySpan(ref label, 1);
            RecordMeasurement(val, labels);
        }

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void RecordMeasurement(T val,
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void RecordMeasurement(T val, ReadOnlySpan<(string LabelName, string LabelValue)> labels)
        {
            // this captures a snapshot, _subscriptions array could be replaced while
            // we are invoking callbacks
            ListenerSubscription[] subscriptions = _subscriptions;
            for (int i = 0; i < subscriptions.Length; i++)
            {
                // All these conditionals can be resolved statically by the JIT once it knows the T type.
                // The body of the loop reduces to just the statement from one branch and all the rest are eliminated
                if (val is double dVal)
                {
                    subscriptions[i].Listener.MeasurementRecorded(this, dVal, labels, subscriptions[i].Cookie);
                }
                else if (val is float fVal)
                {
                    subscriptions[i].Listener.MeasurementRecorded(this, fVal, labels, subscriptions[i].Cookie);
                }
                else if (val is long lVal)
                {
                    subscriptions[i].Listener.MeasurementRecorded(this, lVal, labels, subscriptions[i].Cookie);
                }
                else if (val is int iVal)
                {
                    subscriptions[i].Listener.MeasurementRecorded(this, iVal, labels, subscriptions[i].Cookie);
                }
                else if (val is short sVal)
                {
                    subscriptions[i].Listener.MeasurementRecorded(this, sVal, labels, subscriptions[i].Cookie);
                }
                else if (val is byte bVal)
                {
                    subscriptions[i].Listener.MeasurementRecorded(this, bVal, labels, subscriptions[i].Cookie);
                }
                else
                {
                    subscriptions[i].Listener.MeasurementRecorded(this, val, labels, subscriptions[i].Cookie);
                }
            }
        }

    }
}
