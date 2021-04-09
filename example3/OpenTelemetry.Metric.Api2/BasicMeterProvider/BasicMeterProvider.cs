using System.Collections.Concurrent;

namespace OpenTelemetry.Metric.Api2
{
    public class BasicMeterProvider : IMeterProvider
    {
        private ConcurrentDictionary<(string name, string version), BasicMeter> meters = new ();

        public virtual IMeter GetMeter(string name, string version = null)
        {
            BasicMeter meter = null;
            BasicMeter newMeter = null;

            while (meter is null)
            {
                if (!meters.TryGetValue((name, version), out meter))
                {
                    if (newMeter is null)
                    {
                        newMeter = new BasicMeter(this, name, version);
                    }

                    if (meters.TryAdd((name, version), newMeter))
                    {
                        meter = newMeter;
                    }
                }
            }

            return meter;
        }
    }
}
