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


        public override void Update(double value)
        {
            this._lastValue = value;
        }

        public override AggregationStatistics Collect()
        {
            return new AggregationStatistics(MeasurementAggregation, "last", _lastValue.ToString());
        }
    }
}
