using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Diagnostics.Metric;

namespace OpenTelemetry.Metric.Sdk
{
    /// <summary>
    /// Stores Aggregators for an arbitrary number of LabelSets of varying cardinality
    /// The implementation has several optimizations to reduce allocations, storage size, and
    /// CPU usage when Update() is used with label sets that have small and consistent size.
    /// </summary>
    /// <typeparam name="TAggregator"></typeparam>
    struct MultiSizeAggregationStore<TAggregator> where TAggregator : Aggregator, new()
    {
        // this can be:
        // null - if Update() has never been called
        // TAggregator if Update() has only been called with no labels
        // ConcurrentDictionary<LabelSet1, TAggregator> - if Update() has only been called with 1 label
        // ConcurrentDictionary<LabelSet2, TAggregator> - if Update() has only been called with 2 labels
        // ConcurrentDictionary<MetricLabelSet, TAggregator> - if Update() has only been called with >2 labels
        // MultiSizedLabelDictionaries<TAggregator> - if Update() has been called with numbers of labels that fall into more
        //                                            than one of the other categories
        volatile object _stateUnion;

        public void UpdateNoLabels(double measurement)
        {
            TAggregator aggregator;
            while (true)
            {
                object stateUnionCapture = _stateUnion;
                if (stateUnionCapture is TAggregator)
                {
                    aggregator = (TAggregator)stateUnionCapture;
                    break;
                }
                else if (stateUnionCapture is MultiSizedLabelDictionaries<TAggregator> multiSize)
                {
                    aggregator = multiSize.NoLabelAggregator;
                    if (aggregator == null)
                    {
                        Interlocked.CompareExchange(ref multiSize.NoLabelAggregator, new TAggregator(), null);
                        aggregator = multiSize.NoLabelAggregator;
                    }
                    break;
                }
                else if (stateUnionCapture == null)
                {
                    Interlocked.CompareExchange(ref _stateUnion, new TAggregator(), stateUnionCapture);
                    continue;
                }
                else
                {
                    Interlocked.CompareExchange(ref _stateUnion, new MultiSizedLabelDictionaries<TAggregator>(stateUnionCapture), stateUnionCapture);
                    continue;
                }
            }
            aggregator.Update(measurement);
        }

        public void Update1Label(double measurement, (string LabelName, string LabelValue) label1)
        {
            Update1Label(measurement, new LabelSet1(label1));
        }

        public void Update1Label(double measurement, LabelSet1 labels)
        {
            ConcurrentDictionary<LabelSet1, TAggregator> aggregators;
            while (true)
            {
                object stateUnionCapture = _stateUnion;
                if (stateUnionCapture is ConcurrentDictionary<LabelSet1, TAggregator>)
                {
                    aggregators = (ConcurrentDictionary<LabelSet1, TAggregator>)stateUnionCapture;
                    break;
                }
                else if (stateUnionCapture is MultiSizedLabelDictionaries<TAggregator> multiSize)
                {
                    aggregators = multiSize.Label1;
                    if (aggregators == null)
                    {
                        Interlocked.CompareExchange(ref multiSize.Label1, new ConcurrentDictionary<LabelSet1, TAggregator>(), null);
                        aggregators = multiSize.Label1;
                    }
                    break;
                }
                else if (stateUnionCapture == null)
                {
                    Interlocked.CompareExchange(ref _stateUnion, new ConcurrentDictionary<LabelSet1, TAggregator>(), stateUnionCapture);
                    continue;
                }
                else
                {
                    Interlocked.CompareExchange(ref _stateUnion, new MultiSizedLabelDictionaries<TAggregator>(stateUnionCapture), stateUnionCapture);
                    continue;
                }
            }

            if (!aggregators.TryGetValue(labels, out TAggregator aggregator))
            {
                aggregators.TryAdd(labels, new TAggregator());
                aggregator = aggregators[labels];
            }
            aggregator.Update(measurement);
        }

        public void Update2LabelsUnsorted(double measurement,
            (string LabelName, string LabelValue) label1,
            (string LabelName, string LabelValue) label2)
        {
            if(label1.LabelName.CompareTo(label2.LabelName) <= 0)
            {
                Update2LabelsSorted(measurement, new LabelSet2(label1, label2));
            }
            else
            {
                Update2LabelsSorted(measurement, new LabelSet2(label2, label1));
            }
        }

        public void Update2LabelsSorted(double measurement, LabelSet2 labels)
        {
            ConcurrentDictionary<LabelSet2, TAggregator> aggregators;
            while (true)
            {
                object stateUnionCapture = _stateUnion;
                if (stateUnionCapture is ConcurrentDictionary<LabelSet2, TAggregator>)
                {
                    aggregators = (ConcurrentDictionary<LabelSet2, TAggregator>)stateUnionCapture;
                    break;
                }
                else if (stateUnionCapture is MultiSizedLabelDictionaries<TAggregator> multiSize)
                {
                    aggregators = multiSize.Label2;
                    if (aggregators == null)
                    {
                        Interlocked.CompareExchange(ref multiSize.Label2, new ConcurrentDictionary<LabelSet2, TAggregator>(), null);
                        aggregators = multiSize.Label2;
                    }
                    break;
                }
                else if (stateUnionCapture == null)
                {
                    Interlocked.CompareExchange(ref _stateUnion, new ConcurrentDictionary<LabelSet2, TAggregator>(), stateUnionCapture);
                    continue;
                }
                else
                {
                    Interlocked.CompareExchange(ref _stateUnion, new MultiSizedLabelDictionaries<TAggregator>(stateUnionCapture), stateUnionCapture);
                    continue;
                }
            }

            if (!aggregators.TryGetValue(labels, out TAggregator aggregator))
            {
                aggregators.TryAdd(labels, new TAggregator());
                aggregator = aggregators[labels];
            }
            aggregator.Update(measurement);
        }

        public void UpdateManyLabelsUnsorted(double measurement, ReadOnlySpan<(string LabelName, string LabelValue)> labels)
        {
            //PERF: this could be optimized to stack allocate up to a fixed size
            (string LabelName, string LabelValue)[] sortedLabels = labels.ToArray();
            Array.Sort(sortedLabels, (a, b) => a.LabelName.CompareTo(b.LabelName));
            UpdateManyLabelsSorted(measurement, new MetricLabelSet(sortedLabels));
        }

        public void UpdateManyLabelsSorted(double measurement, MetricLabelSet labels)
        {
            ConcurrentDictionary<MetricLabelSet, TAggregator> aggregators;
            while (true)
            {
                object stateUnionCapture = _stateUnion;
                if (stateUnionCapture is ConcurrentDictionary<MetricLabelSet, TAggregator>)
                {
                    aggregators = (ConcurrentDictionary<MetricLabelSet, TAggregator>)stateUnionCapture;
                    break;
                }
                else if (stateUnionCapture is MultiSizedLabelDictionaries<TAggregator> multiSize)
                {
                    aggregators = multiSize.LabelMany;
                    if (aggregators == null)
                    {
                        Interlocked.CompareExchange(ref multiSize.LabelMany, new ConcurrentDictionary<MetricLabelSet, TAggregator>(), null);
                        aggregators = multiSize.LabelMany;
                    }
                    break;
                }
                else if (stateUnionCapture == null)
                {
                    Interlocked.CompareExchange(ref _stateUnion, new ConcurrentDictionary<MetricLabelSet, TAggregator>(), stateUnionCapture);
                    continue;
                }
                else
                {
                    Interlocked.CompareExchange(ref _stateUnion, new MultiSizedLabelDictionaries<TAggregator>(stateUnionCapture), stateUnionCapture);
                    continue;
                }
            }

            if (!aggregators.TryGetValue(labels, out TAggregator aggregator))
            {
                aggregators.TryAdd(labels, new TAggregator());
                aggregator = aggregators[labels];
            }
            aggregator.Update(measurement);
        }

        public void Collect(Action<LabeledAggregationStatistics> visitAggregationFunc)
        {
            object stateUnionCapture = _stateUnion;
            if (stateUnionCapture == null)
            {
                return;
            }
            else if (stateUnionCapture is TAggregator aggregator)
            {
                visitAggregationFunc(new LabeledAggregationStatistics(aggregator.Collect()));
            }
            else if (stateUnionCapture is ConcurrentDictionary<LabelSet1, TAggregator> label1Aggregators)
            {
                CollectLabelDictionary(label1Aggregators, visitAggregationFunc);
            }
            else if (stateUnionCapture is ConcurrentDictionary<LabelSet2, TAggregator> label2Aggregators)
            {
                CollectLabelDictionary(label2Aggregators, visitAggregationFunc);
            }
            else if (stateUnionCapture is ConcurrentDictionary<MetricLabelSet, TAggregator> labelManyAggregators)
            {
                CollectLabelDictionary(labelManyAggregators, visitAggregationFunc);
            }
            else
            {
                MultiSizedLabelDictionaries<TAggregator> multiSize = (MultiSizedLabelDictionaries<TAggregator>)stateUnionCapture;
                visitAggregationFunc(new LabeledAggregationStatistics(multiSize.NoLabelAggregator.Collect()));
                CollectLabelDictionary(multiSize.Label1, visitAggregationFunc);
                CollectLabelDictionary(multiSize.Label2, visitAggregationFunc);
                CollectLabelDictionary(multiSize.LabelMany, visitAggregationFunc);
            }
        }

        void CollectLabelDictionary<LabelSetType>(
            ConcurrentDictionary<LabelSetType, TAggregator> aggregations,
            Action<LabeledAggregationStatistics> visitFunc) where LabelSetType : ILabelSet
        {
            if(aggregations == null)
            {
                return;
            }
            foreach (KeyValuePair<LabelSetType, TAggregator> kv in aggregations)
            {
                // For this to work we need static labels and dynamic labels to be separate from one another
                // If the same name can be specified both ways then it is possible to specify the same label set in multiple ways, for example:
                // Static: Color=Red Dynamic: Size=1  ==  Static: Color=Red Dynamic: Color=Red, Size=1
                // This causes different dynamic label sets to converge into the same merged set and not all aggregation functions have
                // well defined merges, such as LastValueAggregation
                //
                // If static and dynamic labels can not be guaranteed distinct then the merge needs to happen at Update rather than at Collect
                // which is going to incur more performance overhead.
                visitFunc(new LabeledAggregationStatistics(kv.Value.Collect(), kv.Key.Labels));
            }
        }
    }

    class MultiSizedLabelDictionaries<TAggregator>
    {
        public TAggregator NoLabelAggregator;
        public ConcurrentDictionary<LabelSet1, TAggregator> Label1;
        public ConcurrentDictionary<LabelSet2, TAggregator> Label2;
        public ConcurrentDictionary<MetricLabelSet, TAggregator> LabelMany;

        public MultiSizedLabelDictionaries(object unionSlotValue)
        {
            if (unionSlotValue is TAggregator val0)
            {
                NoLabelAggregator = val0;
            }
            else if (unionSlotValue is ConcurrentDictionary<LabelSet1, TAggregator> val1)
            {
                Label1 = val1;
            }
            else if (unionSlotValue is ConcurrentDictionary<LabelSet2, TAggregator> val2)
            {
                Label2 = val2;
            }
            else if (unionSlotValue is ConcurrentDictionary<MetricLabelSet, TAggregator> valMany)
            {
                LabelMany = valMany;
            }
        }
    }
}
