using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Metric
{
    public interface IGauge
    {
    }

    public class Gauge<T> : UnboundMeterInstrument<T>, IGauge where T:struct
    {
        internal Gauge(Meter meter, string name) :
            base(meter, name)
        {
            Publish();
        }

        public void Set(T val)
        {
            RecordMeasurement(val);
        }

        public void Set(T val, params (string LabelName, string LabelValue)[] labels)
        {
            RecordMeasurement(val, labels);
        }
    }
}
