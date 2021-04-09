using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Metric
{
    public class Gauge : UnboundMeterInstrument
    {
        internal Gauge(Meter meter, string name) :
            base(meter, name)
        {
            Publish();
        }

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
