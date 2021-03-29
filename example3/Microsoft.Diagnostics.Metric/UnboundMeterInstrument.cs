using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Linq;

namespace Microsoft.Diagnostics.Metric
{
    public abstract class UnboundMeterInstrument : MeterInstrument
    {
        internal static Dictionary<string, string> EmptyStaticLabels { get; } = new Dictionary<string, string>();
        public override Meter Meter { get; }
        public override string Name { get; }
        public override Dictionary<string, string> StaticLabels { get; }

        protected UnboundMeterInstrument(Meter meter, string name, Dictionary<string,string> staticLabels)
        {
            Meter = meter;
            Name = name;
            StaticLabels = staticLabels ?? EmptyStaticLabels;
            MeterInstrumentCollection.Instance.AddMetric(this);
        }

        
    }
}
