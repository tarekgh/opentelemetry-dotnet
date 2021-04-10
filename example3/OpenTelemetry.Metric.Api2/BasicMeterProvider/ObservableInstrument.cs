using System;

namespace OpenTelemetry.Metric.Api2
{
    public abstract class ObservableInstrument : Instrument
    {
        internal ObservableInstrument(BasicMeter meter, string name, string description, string unit)
            : base(meter, name, description, unit)
        {
        }

        internal virtual void Observe()
        {
        }
    }

    public class ObservableInstrument<T> : ObservableInstrument
    {
        private Action<Observer<T>, object> callback;
        private object userarg;

        internal ObservableInstrument(BasicMeter meter, string name, Action<Observer<T>, object> callback, object userarg, string description, string unit)
            : base(meter, name, description, unit)
        {
            this.callback = callback;
            this.userarg = userarg;
        }

        internal override void Observe()
        {
            var obv = new Observer<T>();

            this.callback(obv, userarg);

            this.Record<T>(obv.Measures.ToArray());
        }
    }
}
