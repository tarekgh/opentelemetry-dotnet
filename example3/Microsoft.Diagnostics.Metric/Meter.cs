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

        public Meter(string name, string version = "") : this(name, version, UnboundMeterInstrument.EmptyStaticLabels) { }

        public Meter(string name, string version, Dictionary<string,string> staticLabels)
        {
            Name = name;
            Version = version;
            StaticLabels = staticLabels;
            lock (MeterInstrumentCollection.Lock)
            {
                MeterInstrumentCollection.Instance.AddMeter(this);
            }
        }

        public Counter CreateCounter(string name)
        {
            return CreateCounter(name, null);
        }

        public Counter CreateCounter(string name, Dictionary<string, string> labels)
        {
            return new Counter(this, name, labels);
        }

        public ObservableCounter CreateObservableCounter(string name, Func<double> observeValue)
        {
            return CreateObservableCounter(name, observeValue, null);
        }

        public ObservableCounter CreateObservableCounter(string name, Func<double> observeValue, Dictionary<string,string> labels)
        {
            return new ObservableCounter(this, name, labels, observeValue);
        }

        public ObservableCounter CreateObservableCounter(string name, Action<MeasurementObserver> observeValues)
        {
            return CreateObservableCounter(name, observeValues, null);
        }

        public ObservableCounter CreateObservableCounter(string name, Action<MeasurementObserver> observeValues, Dictionary<string, string> labels)
        {
            return new ObservableCounter(this, name, labels, observeValues);
        }

        public Gauge CreateGauge(string name)
        {
            return CreateGauge(name, null);
        }

        public Gauge CreateGauge(string name, Dictionary<string, string> labels)
        {
            return new Gauge(this, name, labels);
        }

        public string Name { get; }
        public string Version { get; }
        public IReadOnlyDictionary<string,string> StaticLabels { get; }


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
