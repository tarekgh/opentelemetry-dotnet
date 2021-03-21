using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Metric
{
    public abstract class LabeledMeterInstrument : MeterInstrumentBase
    {
        public string[] LabelValues { get; }

        protected LabeledMeterInstrument(string[] labelValues)
        {
            LabelValues = labelValues;
        }
    }

    public abstract class LabeledMeterInstrument<T> : LabeledMeterInstrument where T : MeterInstrument
    {
        public T Unlabeled { get; }
        public override Meter Meter => Unlabeled.Meter;
        public override string Name => Unlabeled.Name;
        public override Dictionary<string, string> StaticLabels => Unlabeled.StaticLabels;
        public override AggregationConfiguration DefaultAggregation => Unlabeled.DefaultAggregation;
        public override string[] LabelNames => Unlabeled.LabelNames;
        
        protected LabeledMeterInstrument(T unlabeledMeter, string[] labelValues) : base(labelValues)
        {
            Unlabeled = unlabeledMeter;
            MeterInstrumentCollection.Instance.AddMetric(this);
        }
    }
}
