using System;
using System.Threading;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Metric
{
    public class MeterListener : IDisposable
    {
        private static List<Meter> _meters;

        public MeterListener() { }

        public MeterListener(Func<Meter, bool>? shouldListenTo, Action<MeterInstrument>? instrumentEncountered, Action<MeterInstrument>? instrumentDisposed, Action<Meter>? meterDisposed)
        {
            ShouldListenTo = shouldListenTo;
            InstrumentEncountered = instrumentEncountered;
            InstrumentDisposed = instrumentDisposed;
            MeterDisposed = meterDisposed;
        }

        public Func<Meter, bool> ShouldListenTo { get; set; }
        public Action<MeterInstrument> InstrumentEncountered { get; set; }
        public Action<MeterInstrument> InstrumentDisposed { get; set; }
        public Action<Meter> MeterDisposed { get; set; }

        public void Dispose()
        {
            Meter.RemoveListener(this);

            if (_meters is not null)
            {
                lock (_meters)
                {
                    foreach (Meter meter in _meters)
                    {
                        meter.InstanceRemoveListener(this);
                    }
                }

                _meters.Clear();
            }
        }

        internal void AddMeter(Meter meter)
        {
            if (_meters is null)
            {
                Interlocked.CompareExchange(ref _meters, new List<Meter>(), null);
            }

            lock (_meters)
            {
                if (!_meters.Contains(meter))
                {
                    _meters.Add(meter);
                }
            }
        }
    }
}
