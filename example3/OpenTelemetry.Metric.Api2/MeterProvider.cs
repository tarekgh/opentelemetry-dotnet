using System.Threading;

namespace OpenTelemetry.Metric.Api2
{
    public class MeterProvider : IMeterProvider
    {
        private static IMeterProvider defaultProvider = new BasicMeterProvider();

        public MeterProvider()
        {
        }

        public static IMeterProvider Default
        {
            get
            {
                return MeterProvider.defaultProvider;
            }

            set
            {
                MeterProvider.defaultProvider = value;
            }
        }

        public static IMeterProvider SetDefaultProvider(IMeterProvider meterProvider)
        {
            return Interlocked.Exchange(ref MeterProvider.defaultProvider, meterProvider);
        }

        public IMeter GetMeter(string name, string version = null)
        {
            return defaultProvider.GetMeter(name, version);
        }
    }
}
