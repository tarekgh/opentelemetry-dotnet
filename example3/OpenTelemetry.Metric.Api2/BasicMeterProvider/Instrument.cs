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
            int c = 0;
            foreach (var m in measurements)
            {
                var attr = string.Join(",", m.attributes.Select(k => $"{k.name}={k.value}"));
                Console.WriteLine($"Record [{c+1}/{measurements.Length}]: {Name}[{attr}] = {m.value.GetType().Name}/{m.value}");
                c++;
            }
        }

        internal void Record<T>(T value, (string name, object value)[] attributes)
        {
            var attr = string.Join(",", attributes.Select(k => $"{k.name}={k.value}"));
            Console.WriteLine($"Record: {Name}[{attr}] = {value.GetType().Name}/{value}");
        }
    }
}
