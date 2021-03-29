using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Metric
{
    public class ObservableCounter : UnboundMeterInstrument
    {
        // This is either a Func<double> or an Action<MeasurementObserver>
        object _observeValueFunc;

        public ObservableCounter(Meter meter, string name, Dictionary<string, string> staticLabels, Func<double> observeValue) :
            base(meter, name, staticLabels)
        {
            _observeValueFunc = observeValue;
        }

        public ObservableCounter(Meter meter, string name, Dictionary<string, string> staticLabels, Action<MeasurementObserver> observeValues) :
            base(meter, name, staticLabels)
        {
            _observeValueFunc = observeValues;
        }

        public override AggregationConfiguration DefaultAggregation => AggregationConfigurations.Sum;

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
