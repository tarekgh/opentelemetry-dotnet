using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Metric
{
    public interface ICounter
    {
    }

    public class Counter<T> : UnboundMeterInstrument<T>, ICounter where T:struct
    {
        internal Counter(Meter meter, string name) :
            base(meter, name)
        {
            Publish();
        }

        public void Add(T measurement) => RecordMeasurement(measurement);
        public void Add(T measurement,
            (string LabelName, string LabelValue) label1) => RecordMeasurement(measurement, label1);
        public void Add(T measurement,
            (string LabelName, string LabelValue) label1,
            (string LabelName, string LabelValue) label2) => RecordMeasurement(measurement, label1, label2);
        public void Add(T measurement,
            (string LabelName, string LabelValue) label1,
            (string LabelName, string LabelValue) label2,
            (string LabelName, string LabelValue) label3) => RecordMeasurement(measurement, label1, label2, label3);
        public void Add(T measurement, params (string LabelName, string LabelValue)[] labels) => RecordMeasurement(measurement, labels);
    }
}
