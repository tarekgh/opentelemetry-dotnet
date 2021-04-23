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
        protected MeterInstrument(Meter meter, string name, string? description, string? unit)
        {
            Meter = meter;
            Name = name;
            Description = description;
            Unit = unit;
        }

        public Meter Meter { get; }
        public string Name { get; }
        public string Description { get; }
        public string Unit { get; }

        public abstract bool Enabled { get; }

        /// <summary>
        /// Adds the instrument to the list maintained on Meter which in turn
        /// makes it visible to listeners.
        /// </summary>
        protected void Publish() => Meter.PublishInstrument(this);

        public virtual bool IsObservable => false;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Labels
    {
        public (string LabelName, string LabelValue) Label1;
        public (string LabelName, string LabelValue) Label2;
        public (string LabelName, string LabelValue) Label3;
    }

    internal struct ListenerSubscription<T> where T : unmanaged
    {
        internal MeterInstrumentListener<T> Listener { get; set;  }
        internal object Cookie { get; set; }
    }

    public struct MeasurementObservation<T> where T : unmanaged
    {
        public MeasurementObservation((string, string)[]? labels, T value)
        {
            Labels = labels;
            Value = value;
        }

        public (string, string)[] ? Labels { get; }
        public T Value { get; }
    }

    public abstract class MeterObservableInstrument<T> : MeterInstrument where T : unmanaged
    {
        protected MeterObservableInstrument(Meter meter, string name, string? description, string? unit) : base(meter, name, description, unit) { }
        public abstract IEnumerable<MeasurementObservation<T>> Observe();
        public override bool IsObservable => true;
        public override bool Enabled => true;
    }

    public abstract class MeterInstrument<T> : MeterInstrument where T : unmanaged
    {
        protected MeterInstrument(Meter meter, string name, string? description, string? unit) : base(meter, name, description, unit) { }

        private ListenerSubscription<T>[] _subscriptions = Array.Empty<ListenerSubscription<T>>();

        public void AddListener(MeterInstrumentListener<T> listener, object cookie)
        {
            // only push metrics should have subscriptions
            Debug.Assert(!IsObservable);
            Debug.Assert(listener != null);

            // this should only be called under the metric collection lock
            ListenerSubscription<T>[] subs = new ListenerSubscription<T>[_subscriptions.Length + 1];
            Array.Copy(_subscriptions, subs, _subscriptions.Length);
            subs[_subscriptions.Length].Listener = listener;
            subs[_subscriptions.Length].Cookie = cookie;
            _subscriptions = subs;
        }

        public override bool Enabled => _subscriptions.Length > 0;

        /// <summary>
        /// Returns true if the listener was previously subscribed
        /// </summary>
        /// <param name="listener"></param>
        /// <param name="cookie"></param>
        /// <returns></returns>
        internal bool RemoveSubscription(MeterInstrumentListener<T> listener, out object cookie)
        {
            // only push metrics should have subscriptions
            Debug.Assert(!IsObservable);

            // this should only be called under metric collection lock
            for (int i = 0; i < _subscriptions.Length; i++)
            {
                if (_subscriptions[i].Listener == listener)
                {
                    cookie = _subscriptions[i].Cookie;
                    ListenerSubscription<T>[] subs = new ListenerSubscription<T>[_subscriptions.Length - 1];
                    Array.Copy(_subscriptions, subs, i);
                    Array.Copy(_subscriptions, i + 1, subs, i, _subscriptions.Length - i - 1);
                    _subscriptions = subs;
                    return true;
                }
            }
            cookie = null;
            return false;
        }

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void RecordMeasurement(T val, (string LabelName, string LabelValue) label1, (string LabelName, string LabelValue) label2)
        {
            Labels twoLabels = new Labels();
            twoLabels.Label1 = label1;
            twoLabels.Label2 = label2;
            ReadOnlySpan<(string LabelName, string LabelValue)> labels = MemoryMarshal.CreateReadOnlySpan(ref twoLabels.Label1, 2);
            RecordMeasurement(val, labels);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void RecordMeasurement(T val) => RecordMeasurement(val, Array.Empty<(string LabelName, string LabelValue)>());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void RecordMeasurement(T val, (string LabelName, string LabelValue) label)
        {
            ReadOnlySpan<(string LabelName, string LabelValue)> labels = MemoryMarshal.CreateReadOnlySpan(ref label, 1);
            RecordMeasurement(val, labels);
        }

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void RecordMeasurement(T val, (string LabelName, string LabelValue) label1, (string LabelName, string LabelValue) label2, (string LabelName, string LabelValue) label3)
        {
            Labels threeLabels = new Labels();
            threeLabels.Label1 = label1;
            threeLabels.Label2 = label2;
            threeLabels.Label3 = label3;
            ReadOnlySpan<(string LabelName, string LabelValue)> labels = MemoryMarshal.CreateReadOnlySpan(ref threeLabels.Label1, 3);
            RecordMeasurement(val, labels);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void RecordMeasurement(T value, ReadOnlySpan<(string LabelName, string LabelValue)> labels)
        {
            // this captures a snapshot, _subscriptions array could be replaced while
            // we are invoking callbacks
            ListenerSubscription<T>[] subscriptions = _subscriptions;
            for (int i = 0; i < subscriptions.Length; i++)
            {
                subscriptions[i].Listener.MeasurementRecorded(this, value, labels, subscriptions[i].Cookie);
            }
        }
    }
}
