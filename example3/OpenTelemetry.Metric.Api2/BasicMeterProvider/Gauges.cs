using System;

namespace OpenTelemetry.Metric.Api2
{
    public class Gauge : Instrument, IGauge
    {
        internal Gauge(BasicMeter meter, string name, string description, string unit)
            : base(meter, name, description, unit)
        {
        }

        public void Set(double value, params (string name, object value)[] attributes)
        {
            this.Record<double>(value, attributes);
        }
    }

    public class Gauge<T> : Gauge, IGauge<T>
    {
        internal Gauge(BasicMeter meter, string name, string description, string unit)
            : base(meter, name, description, unit)
        {
        }

        public virtual void Set(T value, params (string name, object value)[] attributes)
        {
            this.Record<T>(value, attributes);
        }
    }

    public class GaugeFunc : ObservableInstrument<double>, IGaugeFunc
    {
        internal GaugeFunc(BasicMeter meter, string name, Action<Observer, object> callback, object state, string description, string unit)
            : base(meter, name, callback, state, description, unit)
        {
        }
    }

    public class GaugeFunc<T> : ObservableInstrument<T>, IGaugeFunc<T>
    {
        internal GaugeFunc(BasicMeter meter, string name, Action<Observer, object> callback, object state, string description, string unit)
            : base(meter, name, callback, state, description, unit)
        {
        }
    }
}
