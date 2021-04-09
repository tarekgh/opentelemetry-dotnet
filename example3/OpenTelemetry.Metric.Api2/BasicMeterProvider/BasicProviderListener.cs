using System.Linq;

namespace OpenTelemetry.Metric.Api2
{
    public abstract class BasicMeterProviderListener
    {
        public abstract void Record<T>(Instrument instrument, T value, (string name, object value)[] attributes);

        public abstract void Record<T>(Instrument instrument, (T value, (string name, object value)[] attributes)[] measurements);

        public string ToString<T>(Instrument instrument, int c, int count, T value, (string name, object value)[] attributes)
        {
            var attr = string.Join(",", attributes.Select(k => $"{k.name}={k.value}"));
            var msg = $"[{c+1}/{count}] {instrument.Name}[{attr}] = {instrument.GetType().Name}/{value.GetType().Name}/{value}";
            return msg;
        }
    }
}
