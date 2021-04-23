using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Metric
{
    public class Distribution<T> : MeterInstrument<T> where T: unmanaged
    {
        internal Distribution(Meter meter, string name, string? description, string? unit) : base(meter, name, description, unit)
        {
            Publish();
        }

        public void Record(T measurement) => RecordMeasurement(measurement);
        public void Record(T measurement,
            (string LabelName, string LabelValue) label1) => RecordMeasurement(measurement, label1);
        public void Record(T measurement,
            (string LabelName, string LabelValue) label1,
            (string LabelName, string LabelValue) label2) => RecordMeasurement(measurement, label1, label2);
        public void Record(T measurement,
            (string LabelName, string LabelValue) label1,
            (string LabelName, string LabelValue) label2,
            (string LabelName, string LabelValue) label3) => RecordMeasurement(measurement, label1, label2, label3);
        public void Record(T measurement, params (string LabelName, string LabelValue)[] labels) => RecordMeasurement(measurement, labels);
    }
}
