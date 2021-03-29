using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Metric
{
    public abstract class LabeledMeterInstrument : MeterInstrumentBase
    {
        public ValueTuple<string,string>[] Labels { get; }

        protected LabeledMeterInstrument(ValueTuple<string, string>[] labels)
        {
            Labels = labels;
        }
    }

    public abstract class LabeledMeterInstrument<T> : LabeledMeterInstrument where T : UnboundMeterInstrument
    {
        public T Unlabeled { get; }
        public override Meter Meter => Unlabeled.Meter;
        public override string Name => Unlabeled.Name;
        public override Dictionary<string, string> StaticLabels => Unlabeled.StaticLabels;
        public override AggregationConfiguration DefaultAggregation => Unlabeled.DefaultAggregation;
        
        protected LabeledMeterInstrument(T unlabeledMeter, ValueTuple<string, string>[] labels) : base(labels)
        {
            Unlabeled = unlabeledMeter;
            MeterInstrumentCollection.Instance.AddMetric(this);
        }
    }
}
