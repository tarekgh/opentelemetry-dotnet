using System;
using System.Collections.Concurrent;

namespace OpenTelemetry.Metric.Api2
{
    public class BasicMeter : IMeter
    {
        public ConcurrentDictionary<string, Instrument> Instruments { get; } = new();
        public BasicMeterProvider MyProvider { get; }
        public string Name { get; }
        public string Version { get; }

        internal BasicMeter(BasicMeterProvider provider, string name, string version)
        {
            this.MyProvider = provider;
            this.Name = name;
            this.Version = version;
        }

        public ICounter CreateCounter(string name, string description = null, string unit = null)
        {
            bool isNew = false;

            var instrument = Instruments.GetOrAdd(name, (key) => {
                isNew = true;
                return new Counter(this, key, description, unit);
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

            var instrument = Instruments.GetOrAdd(name, (key) => {
                isNew = true;
                return new Counter<T>(this, key, description, unit);
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

            var instrument = Instruments.GetOrAdd(name, (key) => {
                isNew = true;
                return new CounterFunc(this, key, callback, state, description, unit);
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

            var instrument = Instruments.GetOrAdd(name, (key) => {
                isNew = true;
                return new CounterFunc<T>(this, key, callback, state, description, unit);
            });

            if (!isNew)
            {
                throw new ArgumentException("Instrument name already exists.", nameof(name));
            }

            return (ICounterFunc<T>) instrument;
        }

        public void Observe()
        {
            foreach (var instrument in Instruments)
            {
                if (instrument.Value is ObservableInstrument obv)
                {
                    obv.Observe();
                }
            }
        }
    }
}
