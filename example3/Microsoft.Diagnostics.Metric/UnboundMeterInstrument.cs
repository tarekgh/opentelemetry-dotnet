using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Linq;

namespace Microsoft.Diagnostics.Metric
{
    public abstract class UnboundMeterInstrument : MeterInstrument
    {
        public override Meter Meter { get; }
        public override string Name { get; }

        protected UnboundMeterInstrument(Meter meter, string name)
        {
            Meter = meter;
            Name = name;
        }

        
    }
}
