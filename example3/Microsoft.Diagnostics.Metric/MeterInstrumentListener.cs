using System;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Metric
{
    public delegate void RecordMeasurement<T>(MeterInstrument<T> instrument, T value, ReadOnlySpan<ValueTuple<string, string>> labels, object cookie) where T : unmanaged;

    public class MeterInstrumentListener<T> : IDisposable where T : unmanaged
    {
        private List<MeterInstrument<T>> _subscribedInstruments;

        public MeterInstrumentListener() { }

        public MeterInstrumentListener(RecordMeasurement<T> measurementRecorded)
        {
            MeasurementRecorded = measurementRecorded;
        }

        public RecordMeasurement<T> MeasurementRecorded { get; set;  }

        internal void AddInstrument(MeterInstrument<T> instrument)
        {
            if (_subscribedInstruments is null)
            {
                Interlocked.CompareExchange(ref _subscribedInstruments, new List<MeterInstrument<T>>(), null);
            }

            lock (_subscribedInstruments)
            {
                if (!_subscribedInstruments.Contains(instrument))
                {
                    _subscribedInstruments.Add(instrument);
                }
            }
        }

        public void Dispose()
        {
            List<MeterInstrument<T>> list = _subscribedInstruments;
            if (list is not null)
            {
                foreach (MeterInstrument<T> instrument in list)
                {
                    instrument.RemoveSubscription(this, out _);
                }
            }

            _subscribedInstruments = null;
        }
    }
}
