using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.Metric;
using OpenTelemetry.Metric.Sdk;

namespace OpenTelemetry.Metric.Sdk
{
    internal class SumCountMinMax : Aggregator
    {
        public long count = 0;
        public double sum = 0;
        public double max = 0;
        public double min = 0;

        public override MeasurementAggregation MeasurementAggregation => MeasurementAggregations.Sum;

        public override void Update<T>(T value)
        {
            double dvalue = ToDouble(value);

            count++;
            sum += dvalue;
            if (count == 1)
            {
                min = dvalue;
                max = dvalue;
            }
            else
            {
                min = Math.Min(min, dvalue);
                max = Math.Max(max, dvalue);
            }
        }

        public override AggregationStatistics Collect()
        {
            return new SumCountMinMaxStatistics(sum, count, min, max);
        }
    }
}
