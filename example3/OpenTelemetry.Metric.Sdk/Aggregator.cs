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
    }

    struct AggregationStatistics
    {
        IEnumerable<(string Key, string Value)> _statistics;

        public AggregationStatistics(IEnumerable<(string Key, string Value)> data)
        {
            _statistics = data;
        }

        public AggregationStatistics(params (string Key, string Value)[] statistics)
        {
            _statistics = statistics;
        }

        public AggregationStatistics(string key, string value)
        {
            _statistics = new (string Key, string Value)[] { (key, value) };
        }

        public IEnumerable<(string Key, string Value)> Statistics => _statistics;
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
    }
}
