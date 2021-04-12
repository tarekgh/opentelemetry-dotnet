using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenTelemetry.Metric.Sdk
{

    class InstrumentAggregation
    {
        public InstrumentAggregation(MeasurementAggregation measurementAggregation, LabelAggregation labelAggregation)
        {
            MeasurementAggregation = measurementAggregation;
            LabelAggregation = labelAggregation;
        }
        public MeasurementAggregation MeasurementAggregation { get;}
        public LabelAggregation LabelAggregation { get; }
    }

    public static class MeasurementAggregations
    {
        public static SumAggregation Sum = new SumAggregation();
        public static LastValueAggregation LastValue = new LastValueAggregation();
    }
    
    public class MeasurementAggregation
    {
    }



    public class PercentileAggregation : MeasurementAggregation
    {
        public double[] Percentiles { get; set; }
        public double MaxRelativeError { get; set; } = 0.001;
        public double MinValue { get; set; } = double.MinValue;
        public double MaxValue { get; set; } = double.MaxValue;

    }

    public class SumAggregation : MeasurementAggregation { }

    public class LastValueAggregation : MeasurementAggregation { }

    class LabelAggregation
    {
        public LabelAggregation(bool includeAllCallsiteLabels,
            LabelMapping[] requiredLabels,
            string[] excludedLabels)
        {
            IncludeAllCallsiteLabels = includeAllCallsiteLabels;
            RequiredLabels = requiredLabels;
            ExcludedLabels = excludedLabels;
        }
        public bool IncludeAllCallsiteLabels { get; }
        public string[] ExcludedLabels { get; }

        public LabelMapping[] RequiredLabels { get; }
    }

    struct LabelMapping
    {
        public LabelMapping(string labelName, bool requiredSourceLabel, Func<string,string> computeLabelValue)
        {
            LabelName = labelName;
            RequiresSourceLabel = requiredSourceLabel;
            ComputeLabelValue = computeLabelValue;
        }
        public string LabelName;
        public bool RequiresSourceLabel;
        public Func<string, string> ComputeLabelValue;
    }
}
