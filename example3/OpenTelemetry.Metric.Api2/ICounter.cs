using System;

namespace OpenTelemetry.Metric.Api2
{
    public interface ICounter<T> : IDisposable
    {
        void Add(T value, params (string name, object value)[] attributes);
    }

    public interface ICounter : ICounter<double>
    {
    }
}
