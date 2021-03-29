using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Metric;

namespace OpenTelemetry.Metric.Sdk
{
    abstract class InstrumentState
    {
        public static InstrumentState Create(MeterInstrument instrument)
        {
            AggregationConfiguration config = instrument.DefaultAggregation;
            Type instrumentStateType = typeof(CachedLabelNamesAggregationStore<>).MakeGenericType(GetAggregatorType(config));
            return (InstrumentState) Activator.CreateInstance(instrumentStateType);
        }

        static Type GetAggregatorType(AggregationConfiguration config)
        {
            if (config is SumAggregation)
            {
                return typeof(SumCountMinMax);
            }
            else if (config is LastValueAggregation)
            {
                return typeof(LastValue);
            }
            else
            {
                // for any unsupported aggregations this SDK converts it to SumCountMinMax
                // this is a flexible policy we can make it do whatever we want
                return typeof(SumCountMinMax);
            }
        }

        // This can be called concurrently with Collect()
        public abstract void Update(double measurement, ReadOnlySpan<(string LabelName, string LabelValue)> labels);

        // This can be called concurrently with Update()
        public abstract void Collect(MeterInstrument instrument, Action<LabeledAggregationStatistics> aggregationVisitFunc);
    }


}
