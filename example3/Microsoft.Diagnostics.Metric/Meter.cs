using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Metric
{
    public class Meter : IDisposable
    {
        private static List<MeterListener> s_allMeterListeners = new List<MeterListener>();
        private static List<Meter> s_meters = new List<Meter>();

        private List<MeterListener> _listeners = new List<MeterListener>();
        private List<MeterInstrument> _instruments = new List<MeterInstrument>();

        public Meter(string name) : this(name, "") { }

        public Meter(string name, string version)
        {
            Name = name;
            Version = version;

            lock (s_meters)
            {
                s_meters.Add(this);
            }

            foreach (MeterListener listener in s_allMeterListeners)
            {
                Func<Meter, bool> shouldListTo = listener.ShouldListenTo;
                if (shouldListTo is not null)
                {
                    if (shouldListTo(this))
                    {
                        InstanceAddListener(listener);
                        listener.AddMeter(this);
                    }
                }
            }
        }

        public Counter<Int64> CreateInt64Counter(string name, string? description = null, string? unit = null) => CreateCounter<Int64>(name, description, unit);
        public Counter<double> CreateDoubleCounter(string name, string? description = null, string? unit = null) => CreateCounter<double>(name, description, unit);

        public Counter<T> CreateCounter<T>(string name, string? description = null, string? unit = null) where T : unmanaged
        {
            return new Counter<T>(this, name, description, unit);
        }

        public CounterFunc<Int64> CreateInt64CounterFunc(string name, Func<IEnumerable<MeasurementObservaion<Int64>>> observeValue, string? description = null, string? unit = null) => CreateCounterFunc<Int64>(name, observeValue, description, unit);
        public CounterFunc<double> CreateDoubleCounterFunc(string name, Func<IEnumerable<MeasurementObservaion<double>>> observeValue, string? description = null, string? unit = null) => CreateCounterFunc<double>(name, observeValue, description, unit);

        public CounterFunc<T> CreateCounterFunc<T>(string name, Func<IEnumerable<MeasurementObservaion<T>>> observeValue, string? description = null, string? unit = null) where T : unmanaged
        {
            return new CounterFunc<T>(this, name, observeValue, description, unit);
        }

        public Gauge<Int64> CreateInt64Gauge(string name, string? description = null, string? unit = null) => CreateGauge<Int64>(name, description, unit);
        public Gauge<double> CreateDoubleGauge(string name, string? description = null, string? unit = null) => CreateGauge<double>(name, description, unit);

        public Gauge<T> CreateGauge<T>(string name, string? description = null, string? unit = null) where T : unmanaged
        {
            return new Gauge<T>(this, name, description, unit);
        }

        public Distribution<Int64> CreateInt64Distribution(string name, string? description = null, string? unit = null) => CreateDistribution<Int64>(name, description, unit);
        public Distribution<double> CreateDoubleDistribution(string name, string? description = null, string? unit = null) => CreateDistribution<double>(name, description, unit);

        public Distribution<T> CreateDistribution<T>(string name, string? description = null, string? unit = null) where T : unmanaged
        {
            return new Distribution<T>(this, name, description, unit);
        }

        public string Name { get; }
        public string Version { get; }

        // Called when the instrument get created.
        internal void PublishInstrument(MeterInstrument instrument)
        {
            lock (_instruments)
            {
                _instruments.Add(instrument);
            }

            foreach (MeterListener listener in _listeners)
            {
                Action<MeterInstrument> instrumentEncountered = listener.InstrumentEncountered;
                if (instrumentEncountered is not null)
                {
                    instrumentEncountered(instrument);
                }
            }
        }

        public void Dispose()
        {
            lock (s_meters)
            {
                s_meters.Remove(this);
            }

            foreach (MeterListener listener in _listeners)
            {
                Action<Meter> meterDisposed = listener.MeterDisposed;
                if (meterDisposed is not null)
                {
                    meterDisposed(this);
                }

                Action<MeterInstrument> instrumentDisposed = listener.InstrumentDisposed;
                if (instrumentDisposed is not null)
                {
                    foreach (MeterInstrument instrument in _instruments)
                    {
                        instrumentDisposed(instrument);
                    }
                }
            }

            lock (_listeners)
            {
                _listeners.Clear();
            }

            lock (_instruments)
            {
                _instruments.Clear();
            }
        }

        internal IEnumerable<MeterInstrument> Instruments => (IEnumerable<MeterInstrument>)_instruments ?? Array.Empty<MeterInstrument>();

        public static void AddListener(MeterListener listener)
        {
            if (listener == null) { return; }

            Func<Meter, bool> shouldListTo = listener.ShouldListenTo;
            if (shouldListTo is not null)
            {
                foreach (Meter meter in s_meters)
                {
                    if (shouldListTo(meter))
                    {
                        listener.AddMeter(meter);
                        meter.InstanceAddListener(listener);
                    }
                }
            }

            lock (s_allMeterListeners)
            {
                if (!s_allMeterListeners.Contains(listener))
                {
                    s_allMeterListeners.Add(listener);
                }
            }
        }

        internal void InstanceAddListener(MeterListener listener)
        {
            Debug.Assert(listener is not null);
            lock (_listeners)
            {
                if (!_listeners.Contains(listener))
                {
                    _listeners.Add(listener);
                }
            }

            Action<MeterInstrument> instrumentEncountered = listener.InstrumentEncountered;
            if (instrumentEncountered is not null)
            {
                foreach (MeterInstrument instrument in _instruments)
                {
                    instrumentEncountered(instrument);
                }
            }
        }

        public static void RemoveListener(MeterListener listener)
        {
            lock (s_allMeterListeners)
            {
                s_allMeterListeners.Remove(listener);
            }
        }

        internal void InstanceRemoveListener(MeterListener listener)
        {
            lock (_listeners)
            {
                _listeners.Remove(listener);
            }
        }
    }
}
