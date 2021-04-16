using System.Linq;

namespace OpenTelemetry.Metric.Api2
{
    public class BasicMeterProviderListener
    {
        public virtual void OnCreateMeter(BasicMeter meter)
        {
        }

        public virtual void OnRemoveMeter(BasicMeter meter)
        {
        }

        public virtual object OnCreateInstrument(Instrument instrument)
        {
            return null;
        }

        public virtual void OnRemoveInstrument(Instrument instrument)
        {
        }

        public virtual void Record<T>(Instrument instrument, T value, (string name, object value)[] attributes)
        {
        }

        public virtual void Record<T>(Instrument instrument, (T value, (string name, object value)[] attributes)[] measurements)
        {
        }

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
