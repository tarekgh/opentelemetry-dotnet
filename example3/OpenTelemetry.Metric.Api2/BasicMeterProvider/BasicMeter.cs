using System;
using System.Collections.Concurrent;

namespace OpenTelemetry.Metric.Api2
{
    public class BasicMeter : IMeter
    {
        public ConcurrentDictionary<string, Instrument> Instruments { get; } = new();
        public BasicMeterProvider MyProvider { get; init; }
        public string Name { get; init; }
        public string Version { get; init; }

        internal BasicMeter(BasicMeterProvider provider, string name, string version)
        {
            this.MyProvider = provider;
            this.Name = name;
            this.Version = version;
        }

        public ICounter CreateCounter(string name, string description = null, string unit = null)
        {
            bool isNew = false;

            var instrument = Instruments.GetOrAdd(name, (n) => {
                isNew = true;
                return new Counter(this, name, description, unit);
            });

            if (!isNew)
            {
                throw new ArgumentException("Instrument name already exists.", nameof(name));
            }

            return (ICounter) instrument;
        }

        public ICounter<T> CreateCounter<T>(string name, string description = null, string unit = null)
        {
            bool isNew = false;

            var instrument = Instruments.GetOrAdd(name, (n) => {
                isNew = true;
                return new Counter<T>(this, name, description, unit);
            });

            if (!isNew)
            {
                throw new ArgumentException("Instrument name already exists.", nameof(name));
            }

            return (ICounter<T>) instrument;
        }

        public ICounterFunc CreateCounterFunc(string name, Action<Observer<double>, object> callback, object state, string description = null, string unit = null)
        {
            bool isNew = false;

            var instrument = Instruments.GetOrAdd(name, (n) => {
                isNew = true;
                return new CounterFunc(this, name, callback, state, description, unit);
            });

            if (!isNew)
            {
                throw new ArgumentException("Instrument name already exists.", nameof(name));
            }

            return (ICounterFunc) instrument;
        }

        public ICounterFunc<T> CreateCounterFunc<T>(string name, Action<Observer<T>, object> callback, object state, string description = null, string unit = null)
        {
            bool isNew = false;

            var instrument = Instruments.GetOrAdd(name, (n) => {
                isNew = true;
                return new CounterFunc<T>(this, name, callback, state, description, unit);
            });

            if (!isNew)
            {
                throw new ArgumentException("Instrument name already exists.", nameof(name));
            }

            return (ICounterFunc<T>) instrument;
        }

        public IGauge CreateGauge(string name, string description = null, string unit = null)
        {
            bool isNew = false;

            var instrument = Instruments.GetOrAdd(name, (n) => {
                isNew = true;
                return new Gauge(this, name, description, unit);
            });

            if (!isNew)
            {
                throw new ArgumentException("Instrument name already exists.", nameof(name));
            }

            return (IGauge) instrument;
        }

        public IGauge<T> CreateGauge<T>(string name, string description = null, string unit = null)
        {
            bool isNew = false;

            var instrument = Instruments.GetOrAdd(name, (n) => {
                isNew = true;
                return new Gauge<T>(this, name, description, unit);
            });

            if (!isNew)
            {
                throw new ArgumentException("Instrument name already exists.", nameof(name));
            }

            return (IGauge<T>) instrument;
        }

        public IGaugeFunc CreateGaugeFunc(string name, Action<Observer<double>, object> callback, object state, string description = null, string unit = null)
        {
            bool isNew = false;

            var instrument = Instruments.GetOrAdd(name, (n) => {
                isNew = true;
                return new GaugeFunc(this, name, callback, state, description, unit);
            });

            if (!isNew)
            {
                throw new ArgumentException("Instrument name already exists.", nameof(name));
            }

            return (IGaugeFunc) instrument;
        }

        public IGaugeFunc<T> CreateGaugeFunc<T>(string name, Action<Observer<T>, object> callback, object state, string description = null, string unit = null)
        {
            bool isNew = false;

            var instrument = Instruments.GetOrAdd(name, (n) => {
                isNew = true;
                return new GaugeFunc<T>(this, name, callback, state, description, unit);
            });

            if (!isNew)
            {
                throw new ArgumentException("Instrument name already exists.", nameof(name));
            }

            return (IGaugeFunc<T>) instrument;
        }

        public void Observe()
        {
            foreach (var instrument in Instruments)
            {
                if (instrument.Value is ObservableInstrument<int> obvint)
                {
                    obvint.Observe();
                }
                else if (instrument.Value is ObservableInstrument<long> obvlong)
                {
                    obvlong.Observe();
                }
                else if (instrument.Value is ObservableInstrument<double> obvdouble)
                {
                    obvdouble.Observe();
                }
            }
        }
    }
}
