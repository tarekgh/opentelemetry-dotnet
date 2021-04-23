using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Metric
{
    public class CounterFunc<T> : MeterObservableInstrument<T> where T: unmanaged
    {
        // This is either a Func<double> or an Action<MeasurementObserver>
        Func<IEnumerable<MeasurementObservation<T>>> _observeValueFunc;

        internal CounterFunc(Meter meter, string name, Func<IEnumerable<MeasurementObservation<T>>> observeValues, string? description, string? unit) : base(meter, name, description, unit)
        {
            _observeValueFunc = observeValues;
            Publish();
        }

        public override IEnumerable<MeasurementObservation<T>> Observe() => _observeValueFunc();
    }
}
