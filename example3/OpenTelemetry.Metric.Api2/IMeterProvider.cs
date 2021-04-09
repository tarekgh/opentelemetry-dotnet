namespace OpenTelemetry.Metric.Api2
{
    public interface IMeterProvider
    {
        IMeter GetMeter(string name, string version = null);
    }
}
