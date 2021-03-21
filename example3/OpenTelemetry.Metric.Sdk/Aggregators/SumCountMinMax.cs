using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.Metric;
using OpenTelemetry.Metric.Sdk;

namespace OpenTelemetry.Metric.Sdk
{
    public class SumCountMinMaxState : AggregatorState
    {
        public long count = 0;
        public double sum = 0;
        public double max = 0;
        public double min = 0;

        public override void Update(MeterInstrumentBase meter, double value)
        {
            count++;
            sum += value;
            if (count == 1)
            {
                min = value;
                max = value;
            }
            else
            {
                min = Math.Min(min, value);
                max = Math.Max(max, value);
            }
        }

        public override (string key, string value)[] Serialize()
        {
            return new (string key, string value)[]
            {
                ( "count", $"{count}" ),
                ( "sum", $"{sum}" ),
                ( "min", $"{min}" ),
                ( "max", $"{max}" )
            };
        }
    }
}
