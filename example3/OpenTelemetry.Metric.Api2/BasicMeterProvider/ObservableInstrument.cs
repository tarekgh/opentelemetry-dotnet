using System;

namespace OpenTelemetry.Metric.Api2
{
    public class ObservableInstrument<T> : Instrument
    {
        internal Action<Observer<T>, object> callback;
        internal object userarg;

        public ObservableInstrument(BasicMeter meter, string name, Action<Observer<T>, object> callback, object userarg, string description, string unit)
            : base(meter, name, description, unit)
        {
            this.callback = callback;
            this.userarg = userarg;
        }

        internal virtual void Observe()
        {
            var obv = new Observer<T>();
            this.callback(obv, userarg);

            this.Record<T>(obv.measures.ToArray());
        }
    }
}
