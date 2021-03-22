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

        public void Init() { }

        public override void Update(double value)
        {
            this._lastValue = value;
        }

        public override AggregationStatistics Collect()
        {
            return new AggregationStatistics("last", _lastValue.ToString());
        }
    }
}
