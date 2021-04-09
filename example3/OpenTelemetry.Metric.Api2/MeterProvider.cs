namespace OpenTelemetry.Metric.Api2
{
    public class MeterProvider : IMeterProvider
    {
        public static IMeterProvider Default { get; set; } = new BasicMeterProvider();

        private IMeterProvider provider;

        public MeterProvider()
        {
            this.provider = new BasicMeterProvider();
        }

        public MeterProvider(IMeterProvider provider)
        {
            this.provider = provider;
        }

        public IMeter GetMeter(string name, string version = null)
        {
            return this.provider.GetMeter(name, version);
        }
    }
}
