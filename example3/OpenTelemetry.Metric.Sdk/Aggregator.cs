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
        public abstract void Update(double num);

        // This can be called concurrently with Update()
        public abstract AggregationStatistics Collect();

        public abstract MeasurementAggregation MeasurementAggregation { get; }
    }

    struct AggregationStatistics
    {
        public AggregationStatistics(MeasurementAggregation measurementAggregation, IEnumerable<(string Key, string Value)> data)
        {
            Statistics = data;
            MeasurementAggregation = measurementAggregation;
        }

        public AggregationStatistics(MeasurementAggregation measurementAggregation, params (string Key, string Value)[] statistics) :
            this(measurementAggregation, (IEnumerable<(string Key, string Value)>)statistics)
        { }

        public AggregationStatistics(MeasurementAggregation measurementAggregation, string key, string value) :
            this(measurementAggregation, new (string Key, string Value)[] { (key, value) })
        { }

        public IEnumerable<(string Key, string Value)> Statistics { get; }
        public MeasurementAggregation MeasurementAggregation { get; }
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

        public LabeledAggregationStatistics WithLabels(IEnumerable<KeyValuePair<string,string>> labels)
        {
            return new LabeledAggregationStatistics(_aggStats, Labels.Concat(labels.Select(kv => (kv.Key,kv.Value))));
        }

        public IEnumerable<(string LabelName, string LabelValue)> Labels => _labels;
        public IEnumerable<(string Key, string Value)> Statistics => _aggStats.Statistics;
        public MeasurementAggregation MeasurementAggregation => _aggStats.MeasurementAggregation;
    }
}
