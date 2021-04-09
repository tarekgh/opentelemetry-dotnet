using System;

namespace OpenTelemetry.Metric.Api2
{
    public interface IMeter
    {
        ICounter CreateCounter(string name, string description = null, string unit = null);
        ICounter<T> CreateCounter<T>(string name, string description = null, string unit = null);

        ICounterFunc CreateCounterFunc(string name, Action<Observer<double>, object> callback, object state = null, string description = null, string unit = null);
        ICounterFunc<T> CreateCounterFunc<T>(string name, Action<Observer<T>, object> callback, object state = null, string description = null, string unit = null);

        IGauge CreateGauge(string name, string description = null, string unit = null);
        IGauge<T> CreateGauge<T>(string name, string description = null, string unit = null);
        
        IGaugeFunc CreateGaugeFunc(string name, Action<Observer<double>, object> callback, object state = null, string description = null, string unit = null);
        IGaugeFunc<T> CreateGaugeFunc<T>(string name, Action<Observer<T>, object> callback, object state = null, string description = null, string unit = null);
    }
}
