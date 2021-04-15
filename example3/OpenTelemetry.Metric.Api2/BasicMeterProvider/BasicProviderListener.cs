using System.Linq;

namespace OpenTelemetry.Metric.Api2
{
    public abstract class BasicMeterProviderListener
    {
        public abstract object OnCreateInstrument(Instrument instrument);

        public abstract void Record<T>(Instrument instrument, T value, (string name, object value)[] attributes);

        public abstract void Record<T>(Instrument instrument, (T value, (string name, object value)[] attributes)[] measurements);

        public string ToString<T>(Instrument instrument, T value, (string name, object value)[] attributes)
        {
            var ver = instrument.MyMeter.Version is null ? "" : $"/ver={instrument.MyMeter.Version}";
            var desc = instrument.Description is null ? "" : $"/desc={instrument.Description}";
            var unit = instrument.Unit is null ? "" : $"/unit={instrument.Unit}";
            var attr = string.Join(",", attributes.Select(k => $"{k.name}={k.value}"));
            var msg = $"[lib={instrument.MyMeter.Name}{ver}] [kind={instrument.GetType().Name}/dtype={value.GetType().Name}{unit}{desc}] {instrument.Name}[{attr}]={value}";
            return msg;
        }
    }
}
