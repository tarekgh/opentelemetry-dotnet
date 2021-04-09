namespace OpenTelemetry.Metric.Api2
{
    public interface IGauge : IGauge<double>
    {
    }

    public interface IGauge<T>
    {
        void Set(T value, params (string name, object value)[] attributes);
    }
}
