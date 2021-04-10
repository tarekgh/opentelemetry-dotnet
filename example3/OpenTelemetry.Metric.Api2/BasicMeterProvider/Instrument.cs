using System;

namespace OpenTelemetry.Metric.Api2
{
    public class Instrument : IDisposable
    {
        public BasicMeter MyMeter { get; }
        public string Name { get; }
        public string Description { get; }
        public string Unit { get; }

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

        public void Dispose()
        {
            MyMeter.RemoveInstrument(this.Name);
        }
    }
}
