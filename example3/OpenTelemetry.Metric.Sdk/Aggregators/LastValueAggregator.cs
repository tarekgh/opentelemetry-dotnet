using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Diagnostics.Metric;
using OpenTelemetry.Metric.Sdk;

namespace OpenTelemetry.Metric.Sdk
{
    public class LastValueState : AggregatorState
    {
        private double dvalue = 0.0;

        public override void Update(MeterInstrumentBase meter, double value)
        {
            this.dvalue = value;
        }

        public override (string key, string value)[] Serialize()
        {
            return new (string key, string value)[]
            {
                ( "last", dvalue.ToString() ),
            };
        }
    }
}
