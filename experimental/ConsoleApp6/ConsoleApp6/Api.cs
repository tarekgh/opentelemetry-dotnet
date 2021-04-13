using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace System.Diagnostics
{
    public class MetricSource
    {
        public MetricSource(string componentName, Version version = default, IDictionary<string, string> defaultLabels = null) { }

        public Counter CreateCounter(string counterName, IDictionary<string, string> defaultLabels = null)
        {
            Metric m = new Metric(this, counterName, MetricAggregations.Sum, defaultLabels);
            return new Counter(m);
        }

        public Gauge CreateGauge(string counterName, IDictionary<string, string> defaultLabels = null)
        {
            Metric m = new Metric(this, counterName, MetricAggregations.Sum, defaultLabels);
            return new Gauge(m);
        }
    }


    //TODO: do we have different metric types or a generic parameter to allow collection of different integer/float sizes?
    public class Metric
    {
        internal Metric(MetricSource source,
                        string name,
                        MetricAggregation defaultAggregation,
                        IDictionary<string, string> defaultLabels)
        {
            Source = source;
            Name = name;
            DefaultAggregation = defaultAggregation;
            DefaultLabels = new LabelSet(defaultLabels);
        }

        public MetricSource Source { get; }
        public string Name { get; }
        public MetricAggregation DefaultAggregation { get; }
        public LabelSet DefaultLabels { get; }

        public void Record(int measurement) { }
        public void Record(int measurement, string labelName1, string labelValue1) { }
        public void Record(int measurement, string labelName1, string labelValue1, string labelName2, string labelValue2) { }
        public void Record(int measurement, string labelName1, string labelValue1, string labelName2, string labelValue2, string labelName3, string labelValue3) { }
        public void Record(int measurement, string labelName1, string labelValue1, string labelName2, string labelValue2, string labelName3, string labelValue3, params string[] additionalLabels) { }
        public void Record(int measurement, LabelSet labels)
        { }

    }

    //TODO: we could derive from Metric but that would expose the Record() API. Does that matter?
    //TODO: If we don't derive from Metric perhaps we should replicate the other properties?
    public struct Counter
    {
        internal Counter(Metric metric) { Metric = metric; }
        private Metric Metric { get; }
        public void Add(int measurement) =>
            Metric.Record(measurement);
        public void Add(int measurement, string labelName1, string labelValue1) =>
            Metric.Record(measurement, labelName1, labelValue1);
        public void Add(int measurement, string labelName1, string labelValue1, string labelName2, string labelValue2) =>
            Metric.Record(measurement, labelName1, labelValue1, labelName2, labelValue2);
        public void Add(int measurement, string labelName1, string labelValue1, string labelName2, string labelValue2, string labelName3, string labelValue3) =>
            Metric.Record(measurement, labelName1, labelValue1, labelName2, labelValue2, labelName3, labelValue3);
        public void Add(int measurement, string labelName1, string labelValue1, string labelName2, string labelValue2, string labelName3, string labelValue3,
            params string[] additionalLabels) =>
            Metric.Record(measurement, labelName1, labelValue1, labelName2, labelValue2, labelName3, labelValue3, additionalLabels);
        public void Add(int measurement, LabelSet labels) =>
            Metric.Record(measurement, labels);
    }

    public struct Gauge
    {
        internal Gauge(Metric metric) { Metric = metric; }
        private Metric Metric { get; }
        public void Set(int measurement) =>
            Metric.Record(measurement);
        public void Set(int measurement, string labelName1, string labelValue1) =>
            Metric.Record(measurement, labelName1, labelValue1);
        public void Set(int measurement, string labelName1, string labelValue1, string labelName2, string labelValue2) =>
            Metric.Record(measurement, labelName1, labelValue1, labelName2, labelValue2);
        public void Set(int measurement, string labelName1, string labelValue1, string labelName2, string labelValue2, string labelName3, string labelValue3) =>
            Metric.Record(measurement, labelName1, labelValue1, labelName2, labelValue2, labelName3, labelValue3);
        public void Set(int measurement, string labelName1, string labelValue1, string labelName2, string labelValue2, string labelName3, string labelValue3,
            params string[] additionalLabels) =>
            Metric.Record(measurement, labelName1, labelValue1, labelName2, labelValue2, labelName3, labelValue3, additionalLabels);
        public void Set(int measurement, LabelSet labels) =>
            Metric.Record(measurement, labels);
    }


    //TODO: ReadOnlyDictionary, ImmutableDictionary, or something else?
    public class LabelSet : ReadOnlyDictionary<string, string>
    {
        public LabelSet(IDictionary<string, string> labels) : base(labels) { }
        public LabelSet(string labelName1, string labelValue1) : base(null) { }
        public LabelSet(string labelName1, string labelValue1, string labelName2, string labelValue2) : base(null) { }
        public LabelSet(string labelName1, string labelValue1, string labelName2, string labelValue2, string labelName3, string labelValue3) : base(null) { }
    }

    public static class MetricAggregations
    {
        public static SumAggregation Sum = new SumAggregation();
        public static LastValueAggregation LastValue = new LastValueAggregation();
    }
    public class MetricAggregation { }
    public class SumAggregation : MetricAggregation { }
    public class LastValueAggregation : MetricAggregation { }
}
