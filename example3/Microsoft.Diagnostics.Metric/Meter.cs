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

        public Counter CreateCounter(string name)
        {
            return new Counter(this, name);
        }

        public CounterFunc CreateCounterFunc(string name, Func<double> observeValue)
        {
            return new CounterFunc(this, name, observeValue);
        }

        public CounterFunc CreateCounterFunc(string name, Action<MeasurementObserver> observeValues)
        {
            return new CounterFunc(this, name, observeValues);
        }

        public Gauge CreateGauge(string name)
        {
            return new Gauge(this, name);
        }

        public Distribution CreateDistribution(string name)
        {
            return new Distribution(this, name);
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
            lock(MeterInstrumentCollection.Lock)
            {
                MeterInstrumentCollection.Instance.RemoveMeter(this);
                _instruments = null;
            }
        }

        internal IEnumerable<MeterInstrument> Instruments =>
            (IEnumerable<MeterInstrument>)_instruments ?? Array.Empty<MeterInstrument>();
    }
}
