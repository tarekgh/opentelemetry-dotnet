using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Reflection;

namespace OpenTelemetry.Metric.Api2
{
    public class BasicMeterProvider : IMeterProvider
    {
        public BasicMeterProviderListener ProviderListener { get; set; }

        private ConcurrentDictionary<BasicMeter, bool> meters { get; } = new ();

        public BasicMeterProvider()
        {
        }

        public ICollection<BasicMeter> Meters {
            get => meters.Keys;
        }

        public virtual IMeter GetMeter(string name, string version = null)
        {
            var meter = new BasicMeter(this, name, version);
            meters[meter] = true;

            return meter;
        }

        public IMeter GetMeter(Type clazzType)
        {
            Assembly asm = clazzType.Assembly;

            string name = clazzType.FullName;
            var asmVersion = asm.GetName().Version?.ToString();
            var fileVersion = asm.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
            var productVersion = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            var version = productVersion ?? asmVersion ?? fileVersion ?? "";

            var meter = new BasicMeter(this, name, version);
            meters[meter] = true;

            return meter;
        }

        public void Observe()
        {
            foreach (var meter in this.meters.Keys)
            {
                meter.Observe();
            }
        }

        internal void RemoveMeter(BasicMeter meter)
        {
            this.meters.Remove(meter, out var value);
        }
    }
}
