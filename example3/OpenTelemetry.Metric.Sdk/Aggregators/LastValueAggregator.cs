using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Diagnostics.Metric;
using OpenTelemetry.Metric.Sdk;

namespace OpenTelemetry.Metric.Sdk
{
    internal class LastValue : Aggregator
    {
        private double _lastValue;

        public override MeasurementAggregation MeasurementAggregation => MeasurementAggregations.LastValue;


        public override void Update<T>(T value)
        {
            this._lastValue = ToDouble(value);
        }

        public override AggregationStatistics Collect()
        {
            return new LastValueStatistics(_lastValue);
        }
    }
}
