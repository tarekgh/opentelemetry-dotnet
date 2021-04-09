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
            AggregationConfiguration config = GetDefaultAggregation(instrument);
            Type instrumentStateType = typeof(CachedLabelNamesAggregationStore<>).MakeGenericType(GetAggregatorType(config));
            return (InstrumentState) Activator.CreateInstance(instrumentStateType);
        }


        public static AggregationConfiguration GetDefaultAggregation(MeterInstrument instrument)
        {
            // In the future instruments will likely have a more explicit default aggregation configuration API
            // but for now the type of the instrument implies the config
            //
            if(instrument is Counter)
            {
                return AggregationConfigurations.Sum;
            }
            else if (instrument is CounterFunc)
            {
                return AggregationConfigurations.Sum;
            }
            else if(instrument is Gauge)
            {
                return AggregationConfigurations.LastValue;
            }
            else
            {
                // TODO: decide how to handle unknown instrument types
                // This could be an error, drop the data silently, or handle it
                // in some default way
                return null;
            }
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
