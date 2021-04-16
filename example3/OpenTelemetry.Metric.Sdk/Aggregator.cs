using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.Metric;
using OpenTelemetry.Metric.Sdk;

namespace OpenTelemetry.Metric.Sdk
{

    internal abstract class Aggregator
    {
        // This can be called concurrently with Collect()
        public abstract void Update<T>(T measurement);

        // This can be called concurrently with Update()
        public abstract AggregationStatistics Collect();

        public abstract MeasurementAggregation MeasurementAggregation { get; }

        public double ToDouble<T>(T value)
        {
            double dvalue = 0;
            if (value is double dval)
            {
                dvalue = dval;
            }
            else if (value is long lval)
            {
                dvalue = lval;
            }
            else if (value is int ival)
            {
                dvalue = ival;
            }

            return dvalue;
        }
    }

    public abstract class AggregationStatistics
    {
        protected AggregationStatistics(MeasurementAggregation measurementAggregation)
        {
            MeasurementAggregation = measurementAggregation;
        }

        public abstract IEnumerable<(string name, string value)> Statistics { get; }
        public MeasurementAggregation MeasurementAggregation { get; }
    }

    public struct QuantileValue
    {
        public QuantileValue(double quantile, double value)
        {
            Quantile = quantile;
            Value = value;
        }
        public double Quantile { get; }
        public double Value { get; }
    }

    public class DistributionStatistics : AggregationStatistics
    {
        internal DistributionStatistics(MeasurementAggregation measurementAggregation, QuantileValue[] quantiles) :
            base(measurementAggregation)
        {
            Quantiles = quantiles;
        }

        public QuantileValue[] Quantiles { get; }

        public override IEnumerable<(string name, string value)> Statistics => Quantiles.Select(qv => ($"quantile_{qv.Quantile}", qv.Value.ToString()));
    }

    public class SumCountMinMaxStatistics : AggregationStatistics
    {
        public SumCountMinMaxStatistics(double sum, double count, double min, double max)
            : base(MeasurementAggregations.Sum)
        {
            Sum = sum;
            Count = count;
            Min = min;
            Max = max;
        }

        public double Sum { get; }
        public double Count { get; }
        public double Min { get; }
        public double Max { get; }

        public override IEnumerable<(string name, string value)> Statistics =>
            new (string name, string value)[]
            {
                ("sum", Sum.ToString()),
                ("count", Count.ToString()),
                ("min", Min.ToString()),
                ("max", Max.ToString())
            };
    }

    class LastValueStatistics : AggregationStatistics
    {
        internal LastValueStatistics(double lastValue) :
            base(MeasurementAggregations.LastValue)
        {
            LastValue = lastValue;
        }

        public double LastValue { get; }

        public override IEnumerable<(string name, string value)> Statistics =>
            new (string name, string value)[]
            {
                ("last", LastValue.ToString())
            };
    }

    class LabeledAggregationStatistics
    {
        AggregationStatistics _aggStats;
        IEnumerable<(string LabelName, string LabelValue)> _labels;

        public LabeledAggregationStatistics(AggregationStatistics stats, params (string LabelName, string LabelValue)[] labels)
        {
            _aggStats = stats;
            _labels = labels;
        }

        public LabeledAggregationStatistics(AggregationStatistics stats, IEnumerable<(string LabelName, string LabelValue)> labels)
        {
            _aggStats = stats;
            _labels = labels;
        }

        public LabeledAggregationStatistics WithLabels(IEnumerable<KeyValuePair<string, string>> labels)
        {
            return new LabeledAggregationStatistics(_aggStats, Labels.Concat(labels.Select(kv => (kv.Key, kv.Value))));
        }

        public IEnumerable<(string LabelName, string LabelValue)> Labels => _labels;
        public AggregationStatistics AggregationStatistics => _aggStats;
    }
}
