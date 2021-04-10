using System;

namespace OpenTelemetry.Metric.Api2
{
    public interface ICounterFunc<T> : IDisposable
    {
    }

    public interface ICounterFunc : ICounterFunc<double>
    {
    }
}
