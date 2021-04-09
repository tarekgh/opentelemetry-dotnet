using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Metric
{
    public class CounterFunc : UnboundMeterInstrument
    {
        // This is either a Func<double> or an Action<MeasurementObserver>
        object _observeValueFunc;

        public CounterFunc(Meter meter, string name, Func<double> observeValue) :
            base(meter, name)
        {
            _observeValueFunc = observeValue;
            Publish();
        }

        public CounterFunc(Meter meter, string name, Action<MeasurementObserver> observeValues) :
            base(meter, name)
        {
            _observeValueFunc = observeValues;
            Publish();
        }

        protected internal override bool IsObservable => true;

        protected internal override void Observe(MeasurementObserver observer)
        {
            if (_observeValueFunc is Func<double>)
            {
                observer.Observe(((Func<double>)_observeValueFunc)());
            }
            else if((_observeValueFunc is Action<MeasurementObserver>))
            {
                ((Action<MeasurementObserver>)_observeValueFunc)(observer);
            }
        }
    }
}
