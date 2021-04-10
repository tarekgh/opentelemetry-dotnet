namespace OpenTelemetry.Metric.Api2
{
    public class MeterProvider : IMeterProvider
    {
        public static IMeterProvider Default { get; set; } = new BasicMeterProvider();

        public IMeterProvider Provider { get; }

        public MeterProvider()
        {
            this.Provider = new BasicMeterProvider();
        }

        public MeterProvider(IMeterProvider provider)
        {
            this.Provider = provider;
        }

        public IMeter GetMeter(string name, string version = null)
        {
            return this.Provider.GetMeter(name, version);
        }
    }
}
