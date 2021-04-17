using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.Metric;
using OpenTelemetry.Metric.Sdk;

namespace OpenTelemetry.Metric.Sdk
{
    class LabelHistogramState : Aggregator
    {
        public Dictionary<string, int> bins = new();

        public override MeasurementAggregation MeasurementAggregation => throw new NotImplementedException();

        public override void Update(double value)
        {
            throw new NotImplementedException();
            /*
            var effectiveLabels = new Dictionary<string,string>();

            var boundLabels = meter.Labels.GetLabels();
            foreach (var label in boundLabels)
            {
                effectiveLabels[label.Item1] = label.Item2;
            }

            var adhocLabels = labels.GetLabels();
            foreach (var label in adhocLabels)
            {
                effectiveLabels[label.Item1] = label.Item2;
            }

            var keys = new List<string>() { "_total" };

            foreach (var l in effectiveLabels)
            {
                keys.Add($"{l.Key}:{l.Value}");
            }

            foreach (var key in keys)
            {
                int count;
                if (!bins.TryGetValue(key, out count))
                {
                    count = 0;
                }

                bins[key] = count + 1;
            }*/
        }

        public override AggregationStatistics Collect()
        {
            var ret = new List<(string, string)>();
            foreach (var bin in bins)
            {
                ret.Add((bin.Key, $"{bin.Value}"));
            }

            throw new NotImplementedException();
        }
    }
}
