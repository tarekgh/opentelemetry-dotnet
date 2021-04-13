using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Metric
{
    public class Distribution : UnboundMeterInstrument
    {
        internal Distribution(Meter meter, string name) :
            base(meter, name)
        {
            Publish();
        }

        public void Record(double measurement) => RecordMeasurement(measurement);
        public void Record(double measurement,
            (string LabelName, string LabelValue) label1) => RecordMeasurement(measurement, label1);
        public void Record(double measurement,
            (string LabelName, string LabelValue) label1,
            (string LabelName, string LabelValue) label2) => RecordMeasurement(measurement, label1, label2);
        public void Record(double measurement,
            (string LabelName, string LabelValue) label1,
            (string LabelName, string LabelValue) label2,
            (string LabelName, string LabelValue) label3) => RecordMeasurement(measurement, label1, label2, label3);
        public void Record(double measurement, params (string LabelName, string LabelValue)[] labels) => RecordMeasurement(measurement, labels);
    }
}