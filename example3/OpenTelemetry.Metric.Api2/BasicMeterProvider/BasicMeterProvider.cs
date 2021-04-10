using System.Collections.Concurrent;

namespace OpenTelemetry.Metric.Api2
{
    public class BasicMeterProvider : IMeterProvider
    {
        public BasicMeterProviderListener ProviderListener { get; set; }

        private ConcurrentBag<BasicMeter> meters = new ();

        public BasicMeterProvider()
        {
        }

        public virtual IMeter GetMeter(string name, string version = null)
        {
            var meter = new BasicMeter(this, name, version);
            meters.Add(meter);

            return meter;
        }
    }
}
