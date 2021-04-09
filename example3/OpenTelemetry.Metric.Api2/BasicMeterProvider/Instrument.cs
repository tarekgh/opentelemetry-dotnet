using System;
using System.Linq;

namespace OpenTelemetry.Metric.Api2
{
    public class Instrument
    {
        public BasicMeter MyMeter { get; init; }
        public string Name { get; init; }
        public string Description { get; init; }
        public string Unit { get; init; }

        public Instrument(BasicMeter meter, string name, string description, string unit)
        {
            this.MyMeter = meter;
            this.Name = name;
            this.Description = description;
            this.Unit = unit;
        }

        internal void Record<T>((T value, (string name, object value)[] attributes)[] measurements)
        {
            var listener = MyMeter.MyProvider.ProviderListener;
            if (listener is not null)
            {
                listener.Record(this, measurements);
            }
        }

        internal void Record<T>(T value, (string name, object value)[] attributes)
        {
            var listener = MyMeter.MyProvider.ProviderListener;
            if (listener is not null)
            {
                listener.Record(this, value, attributes);
            }
        }
    }
}
