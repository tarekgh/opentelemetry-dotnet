using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Metric
{
    public class Meter
    {
        public Meter(string name, string version = "") : this(name, version, UnboundMeterInstrument.EmptyStaticLabels) { }

        public Meter(string name, string version, Dictionary<string,string> staticLabels)
        {
            Name = name;
            Version = version;
            StaticLabels = staticLabels;
        }

        public string Name { get; }
        public string Version { get; }

        public IReadOnlyDictionary<string,string> StaticLabels { get; }
    }
}
