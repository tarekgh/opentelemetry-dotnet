using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Metric
{
    public class Gauge : MeterInstrument
    {
        public Gauge(string name, Meter meter = null) 
            : base(meter, name)
        {
        }

        public Gauge(string name, Dictionary<string, string> staticLabels, Meter meter = null) :
            base(meter, name, staticLabels)
        {
        }

        public override AggregationConfiguration DefaultAggregation => AggregationConfigurations.LastValue;

        public void Set(double d)
        {
            RecordMeasurement(d);
        }

        public void Set(double d, params (string LabelName, string LabelValue)[] labels)
        {
            RecordMeasurement(d, labels);
        }
    }
}
