using System;

namespace OpenTelemetry.Metric.Api2
{
    public class Counter : Instrument, ICounter
    {
        internal Counter(BasicMeter meter, string name, string description, string unit)
            : base(meter, name, description, unit)
        {
        }

        public void Add(double value, params (string name, object value)[] attributes)
        {
            this.Record<double>(value, attributes);
        }
    }

    public class Counter<T> : Counter, ICounter<T>
    {
        internal Counter(BasicMeter meter, string name, string description, string unit)
            : base(meter, name, description, unit)
        {
        }

        public virtual void Add(T value, params (string name, object value)[] attributes)
        {
            this.Record<T>(value, attributes);
        }
    }

    public class CounterFunc : ObservableInstrument<double>, ICounterFunc
    {
        internal CounterFunc(BasicMeter meter, string name, Action<Observer<double>, object> callback, object state, string description, string unit)
            : base(meter, name, callback, state, description, unit)
        {
        }
    }

    public class CounterFunc<T> : ObservableInstrument<T>, ICounterFunc<T>
    {
        internal CounterFunc(BasicMeter meter, string name, Action<Observer<T>, object> callback, object state, string description, string unit)
            : base(meter, name, callback, state, description, unit)
        {
        }
    }
}
