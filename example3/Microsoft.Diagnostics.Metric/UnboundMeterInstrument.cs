using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Microsoft.Diagnostics.Metric
{
    public abstract class UnboundMeterInstrument<T> : MeterInstrument<T> where T:struct
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
