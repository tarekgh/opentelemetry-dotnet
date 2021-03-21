using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Linq;

namespace Microsoft.Diagnostics.Metric
{
    public abstract class MeterInstrument : MeterInstrumentBase
    {
        public static Dictionary<string, string> EmptyStaticLabels { get; } = new Dictionary<string, string>();

        public override Meter Meter { get; }
        public override string Name { get; }

        public override string[] LabelNames { get; }

        public override Dictionary<string, string> StaticLabels { get; }

        protected MeterInstrument(Meter meter, string name, string[] labelNames) 
            : this(meter, name, EmptyStaticLabels, labelNames)
        {
        }

        protected MeterInstrument(Meter meter, string name, Dictionary<string,string> staticLabels, string[] labelNames)
        {
            Meter = meter;
            Name = name;
            StaticLabels = staticLabels;
            LabelNames = labelNames;
            MeterInstrumentCollection.Instance.AddMetric(this);
        }

        
    }
}
