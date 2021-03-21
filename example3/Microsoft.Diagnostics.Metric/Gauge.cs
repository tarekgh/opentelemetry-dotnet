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
            : base(meter, name, Array.Empty<string>())
        {
        }

        public Gauge(string name, Dictionary<string, string> staticLabels, Meter meter = null) :
            base(meter, name, staticLabels, Array.Empty<string>())
        {
        }

        public Gauge(string name, string[] labelNames, Meter meter = null) 
            : base(meter, name, labelNames)
        {
        }

        public Gauge(string name, Dictionary<string,string> staticLabels, string[] labelNames, Meter meter = null) 
            : base(meter, name, staticLabels, labelNames)
        {
        }

        public override AggregationConfiguration DefaultAggregation => AggregationConfigurations.LastValue;

        public void Set(double d) => Set(d, Array.Empty<string>());

        public void Set(double d, params string[] labelValues)
        {
            RecordMeasurement(d, labelValues);
        }
    }
}
