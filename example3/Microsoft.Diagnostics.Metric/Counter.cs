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
            base(meter, name)
        {
        }

        public Counter(string name, Dictionary<string, string> staticLabels, Meter meter = null) :
            base(meter, name, staticLabels)
        {
        }

        public override AggregationConfiguration DefaultAggregation => AggregationConfigurations.Sum;

        public void Add(double measurement) => RecordMeasurement(measurement);
        public void Add(double measurement,
            (string LabelName, string LabelValue) label1) => RecordMeasurement(measurement, label1);
        public void Add(double measurement,
            (string LabelName, string LabelValue) label1,
            (string LabelName, string LabelValue) label2) => RecordMeasurement(measurement, label1, label2);
        public void Add(double measurement, params (string LabelName, string LabelValue)[] labels) => RecordMeasurement(measurement, labels);
        

        public LabeledCounter WithLabels(params (string LabelName, string LabelValue)[] labels)
        {
            return new LabeledCounter(this, labels);
        }
    }

    public class LabeledCounter : LabeledMeterInstrument<Counter>
    {
        internal LabeledCounter(Counter unlabled, (string LabelName, string LabelValue)[] labels) : base(unlabled, labels) { }

        public void Add(double d)
        {
            base.RecordMeasurement(d, Labels);
        }
    }
}
