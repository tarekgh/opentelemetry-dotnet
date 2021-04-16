using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Metric
{
    public class Meter : IDisposable
    {
        List<MeterInstrument> _instruments = new List<MeterInstrument>();

        public Meter(string name) : this(name, "") { }

        public Meter(string name, string version)
        {
            Name = name;
            Version = version;
            lock (MeterInstrumentCollection.Lock)
            {
                MeterInstrumentCollection.Instance.AddMeter(this);
            }
        }

        public Counter<T> CreateCounter<T>(string name) where T:struct
        {
            return new Counter<T>(this, name);
        }

        public CounterFunc<T> CreateCounterFunc<T>(string name, Func<double> observeValue) where T:struct
        {
            return new CounterFunc<T>(this, name, observeValue);
        }

        public CounterFunc<T> CreateCounterFunc<T>(string name, Action<MeasurementObserver> observeValues) where T:struct
        {
            return new CounterFunc<T>(this, name, observeValues);
        }

        public Gauge<T> CreateGauge<T>(string name) where T:struct
        {
            return new Gauge<T>(this, name);
        }

        public Distribution<T> CreateDistribution<T>(string name) where T:struct
        {
            return new Distribution<T>(this, name);
        }

        public string Name { get; }
        public string Version { get; }


        internal void PublishInstrument(MeterInstrument instrument)
        {
            lock (MeterInstrumentCollection.Lock)
            {
                _instruments.Add(instrument);
                MeterInstrumentCollection.Instance.PublishInstrument(instrument);
            }
        }

        public void Dispose()
        {
            lock (MeterInstrumentCollection.Lock)
            {
                MeterInstrumentCollection.Instance.RemoveMeter(this);
                _instruments = null;
            }
        }

        internal IEnumerable<MeterInstrument> Instruments =>
            (IEnumerable<MeterInstrument>)_instruments ?? Array.Empty<MeterInstrument>();
    }
}
