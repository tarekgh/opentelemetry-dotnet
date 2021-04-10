namespace OpenTelemetry.Metric.Api2
{
    public interface ICounterFunc<T>
    {
    }

    public interface ICounterFunc : ICounterFunc<double>
    {
    }
}
