using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Metric
{
    public class Counter : MeterInstrument
    {
        public Counter(string name, Meter meter = null) :
            base(meter, name, Array.Empty<string>())
        {
        }

        public Counter(string name, Dictionary<string, string> staticLabels, Meter meter = null) :
            base(meter, name, staticLabels, Array.Empty<string>())
        {
        }

        public Counter(string name, string[] labelNames, Meter meter = null) :
            base(meter, name, labelNames)
        {
        }

        public Counter(string name, Dictionary<string, string> staticLabels, string[] labelNames, Meter meter = null) :
            base(meter, name, staticLabels, labelNames)
        {
        }

        public override AggregationConfiguration DefaultAggregation => AggregationConfigurations.Sum;

        public void Add(double d) => Add(d, Array.Empty<string>());

        public void Add(double d, params string[] labelValues)
        {
            base.RecordMeasurement(d, labelValues);
        }

        public LabeledCounter WithLabels(params string[] labelValues)
        {
            //TODO: we should probably memoize this
            return new LabeledCounter(this, labelValues);
        }
    }

    public class LabeledCounter : LabeledMeterInstrument<Counter>
    {
        internal LabeledCounter(Counter unlabled, string[] labelValues) : base(unlabled, labelValues) { }

        public void Add(double d)
        {
            base.RecordMeasurement(d, LabelValues);
        }
    }
}
