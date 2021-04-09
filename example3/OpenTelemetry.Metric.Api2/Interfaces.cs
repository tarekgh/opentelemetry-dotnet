using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Linq;

namespace OpenTelemetry.Metric.Api2
{
    public interface IMeterProvider
    {
        IMeter GetMeter(string name, string version = null);
    }

    public interface IMeter
    {
        ICounter CreateCounter(string name, string description = null, string unit = null);
        ICounter<T> CreateCounter<T>(string name, string description = null, string unit = null);

        ICounterFunc CreateCounterFunc(string name, Action<Observer, object> callback, object state = null, string description = null, string unit = null);
        ICounterFunc<T> CreateCounterFunc<T>(string name, Action<Observer, object> callback, object state = null, string description = null, string unit = null);

        IGauge CreateGauge(string name, string description = null, string unit = null);
        IGauge<T> CreateGauge<T>(string name, string description = null, string unit = null);
        
        IGaugeFunc CreateGaugeFunc(string name, Action<Observer, object> callback, object state = null, string description = null, string unit = null);
        IGaugeFunc<T> CreateGaugeFunc<T>(string name, Action<Observer, object> callback, object state = null, string description = null, string unit = null);
    }

    public interface ICounter : ICounter<double>
    {
    }

    public interface ICounter<T>
    {
        void Add(T value, params (string name, object value)[] attributes);
    }

    public interface ICounterFunc : ICounterFunc<double>
    {
    }

    public interface ICounterFunc<T>
    {
    }

    public interface IGauge : IGauge<double>
    {
    }

    public interface IGauge<T>
    {
        void Set(T value, params (string name, object value)[] attributes);
    }

    public interface IGaugeFunc : IGaugeFunc<double>
    {
    }

    public interface IGaugeFunc<T>
    {
    }
}
