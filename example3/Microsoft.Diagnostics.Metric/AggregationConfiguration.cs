using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Metric
{
    public static class AggregationConfigurations
    {
        public static SumAggregation Sum = new SumAggregation();
        public static LastValueAggregation LastValue = new LastValueAggregation();
    }
    
    public class AggregationConfiguration
    {
    }

    public class SumAggregation : AggregationConfiguration { }

    public class LastValueAggregation : AggregationConfiguration { }
}
