using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Diagnostics.Metric;

namespace OpenTelemetry.Metric.Sdk
{
    /// <summary>
    /// This store caches the first set of label names that are observed in Update. All further label sets that have
    /// those same label names will be stored in the cache. Label sets that have additional/fewer/different label names
    /// will be passed through to the MultiSizeAggregationStore.
    /// Label sets that exactly match the sequence of the original names should be fast, sets that have the same names
    /// but in a different order will need to be resorted which is noticably slower.
    ///
    /// This class doesn't handle adding/removing/modifying labels as they pass through but we could create
    /// a modified implementation that did do that. Likely in order from best to worst performance:
    /// a) A fixed number of labels needs to be stored that is known at the time the InstrumentationState is created
    /// b) [This current impl] Number of labels stored = Number of labels provided by Update()
    /// c) Number of labels stored varies based on the names and values of the incoming labels
    ///    For example if the SDK had 'add-or-replace' semantics for a label then N incoming labels could
    ///    produce N or N+1 stored labels depending whether we went down the Add or the Replace path
    /// </summary>
    /// <typeparam name="TAggregator"></typeparam>
    sealed class CachedLabelNamesAggregationStore<TAggregator> : InstrumentState where TAggregator : Aggregator, new()
    {
        volatile LabelSelector[] _cachedLabelNames;
        volatile object _cachedNamesAggregations;
        MultiSizeAggregationStore<TAggregator> _uncachedNamesAggregations;

        public override void Update(double measurement, ReadOnlySpan<(string LabelName, string LabelValue)> labels)
        {
            if(_cachedNamesAggregations == null)
            {
                if(_cachedLabelNames == null)
                {
                    Interlocked.CompareExchange(ref _cachedLabelNames, BuildLabelSort(labels), null);
                }
                int length = _cachedLabelNames.Length;
                Interlocked.CompareExchange(ref _cachedNamesAggregations, CreateLabelValuesDictionary(length), null);
            }

            // These fast paths for 0-2 labels rely on the fact that we won't be adding/removing any labels so the
            // final number of labels that needs to be stored in the dictionary is predictable. The code path
            // in Update3OrMore() is closer to what we'd need if the final stored label count isn't guaranteed to be
            // labels.Length
            if(labels.Length == 0)
            {
                UpdateNoLabels(measurement);
            }
            else if(labels.Length == 1)
            {
                Update1Label(measurement, labels[0]);
            }
            else if(labels.Length == 2)
            {
                Update2Labels(measurement, labels[0], labels[1]);
            }
            else
            {
                Update3OrMoreLabels(measurement, labels);
            }
        }

        void UpdateNoLabels(double measurement)
        {
            if(_cachedLabelNames.Length == 0)
            {
                TAggregator aggregator = (TAggregator)_cachedNamesAggregations;
                aggregator.Update(measurement);
            }
            _uncachedNamesAggregations.UpdateNoLabels(measurement);
        }

        void Update1Label(double measurement, (string LabelName, string LabelValue) label1)
        {
            if (_cachedLabelNames.Length == 1)
            {
                Debug.Assert(_cachedLabelNames[0].Index == 0);
                if(_cachedLabelNames[0].Name == label1.LabelName)
                {
                    ConcurrentDictionary<string, TAggregator> aggregatorDict = (ConcurrentDictionary<string, TAggregator>)_cachedNamesAggregations;
                    if(!aggregatorDict.TryGetValue(label1.LabelValue, out TAggregator aggregator))
                    {
                        aggregatorDict.TryAdd(label1.LabelValue, new TAggregator());
                        aggregator = aggregatorDict[label1.LabelValue];
                    }
                    aggregator.Update(measurement);
                    return;
                }
            }
            _uncachedNamesAggregations.Update1Label(measurement, label1);
        }

        void Update2Labels(double measurement,
            (string LabelName, string LabelValue) label1,
            (string LabelName, string LabelValue) label2)
        {
            if (_cachedLabelNames.Length != 2)
            {
                _uncachedNamesAggregations.Update2LabelsUnsorted(measurement, label1, label2);
                return;
            }

            LabelValues2 key;
            // label names are in sorted order
            if (_cachedLabelNames[0].Name == label1.LabelName &&
                _cachedLabelNames[1].Name == label2.LabelName)
            {
                key = new LabelValues2(label1.LabelValue, label2.LabelValue);
            }
            // label names are in reverse order
            else if(_cachedLabelNames[0].Name == label2.LabelName &&
                    _cachedLabelNames[1].Name == label1.LabelName)
            {
                key = new LabelValues2(label2.LabelValue, label1.LabelValue);
            }
            else
            {
                _uncachedNamesAggregations.Update2LabelsUnsorted(measurement, label1, label2);
                return;
            }
            ConcurrentDictionary<LabelValues2, TAggregator> aggregatorDict =
                (ConcurrentDictionary<LabelValues2, TAggregator>)_cachedNamesAggregations;
            if (!aggregatorDict.TryGetValue(key, out TAggregator aggregator))
            {
                aggregatorDict.TryAdd(key, new TAggregator());
                aggregator = aggregatorDict[key];
            }
            aggregator.Update(measurement);
        }

        void Update3OrMoreLabels(double measurement, ReadOnlySpan<(string LabelName, string LabelValue)> labels)
        {
            if (_cachedLabelNames.Length != labels.Length)
            {
                _uncachedNamesAggregations.UpdateManyLabelsUnsorted(measurement, labels);
                return;
            }

            LabelSelector[] cachedLabelNames = _cachedLabelNames;
            StackStringBuffer buffer = new StackStringBuffer();
            if(!GetSortedLabelValues(cachedLabelNames, labels, ref buffer, out string[] sortedValuesArray))
            {
                // is it the same names but re-ordered? This can have slow path performance but regardless
                // of order any labelset that matches the cached names must get aggregated in the cachedNames
                // dictionary for correctness.
                for (int i = 0; i < cachedLabelNames.Length; i++)
                {
                    int j = 0;
                    for(;j < labels.Length; j++)
                    {
                        if(labels[j].LabelName == cachedLabelNames[i].Name)
                        {
                            break;
                        }
                    }
                    if (j == labels.Length)
                    {
                        // one of the labels we expected to find isn't there, fallback to the uncached store
                        _uncachedNamesAggregations.UpdateManyLabelsUnsorted(measurement, labels);
                        return;
                    }
                }

                // all the labels are there, just not in the order the cache expected. We could implement
                // this performantly if needed, but hopefully we can just discourage users from using
                // variable ordering in performance sensitive code
                LabelSelector[] labelSort = BuildLabelSort(labels);
                bool ret = GetSortedLabelValues(labelSort, labels, ref buffer, out sortedValuesArray);
                Debug.Assert(ret);
            }
            
            if(cachedLabelNames.Length == 3)
            {
                Update3Cached(measurement, buffer.Value1, buffer.Value2, buffer.Value3);
            }
            else
            {
                Debug.Assert(sortedValuesArray != null);
                UpdateManyCached(measurement, sortedValuesArray);
            }
        }

        private void Update3Cached(double measurement, string value1, string value2, string value3)
        {
            ConcurrentDictionary<LabelValues3, TAggregator> aggregatorDict =
                (ConcurrentDictionary<LabelValues3, TAggregator>)_cachedNamesAggregations;
            LabelValues3 key = new LabelValues3(value1, value2, value3);
            if (!aggregatorDict.TryGetValue(key, out TAggregator aggregator))
            {
                aggregatorDict.TryAdd(key, new TAggregator());
                aggregator = aggregatorDict[key];
            }
            aggregator.Update(measurement);
        }

        private void UpdateManyCached(double measurement, string[] values)
        {
            LabelValuesMany key = new LabelValuesMany(values);
            ConcurrentDictionary<LabelValuesMany, TAggregator> aggregatorDict =
                (ConcurrentDictionary<LabelValuesMany, TAggregator>)_cachedNamesAggregations;
            if (!aggregatorDict.TryGetValue(key, out TAggregator aggregator))
            {
                aggregatorDict.TryAdd(key, new TAggregator());
                aggregator = aggregatorDict[key];
            }
            aggregator.Update(measurement);
        }

        /// <summary>
        /// If the labels are in the order corresponding to sort then the label values will be extracted into
        /// a sorted array. If the label names don't match the sort then false is returned.
        /// </summary>
        bool GetSortedLabelValues(
            LabelSelector[] sort,
            ReadOnlySpan<(string LabelName, string LabelValue)> labels,
            ref StackStringBuffer buffer,
            out string[] sortedValuesArray)
        {
            Span<string> values;
            if(sort.Length <= StackStringBuffer.Size)
            {
                values = MemoryMarshal.CreateSpan(ref buffer.Value1, sort.Length);
                sortedValuesArray = null;
            }
            else
            {
                sortedValuesArray = new string[sort.Length];
                values = sortedValuesArray.AsSpan();
            }
            for (int i = 0; i < sort.Length; i++)
            {
                LabelSelector ls = sort[i];
                if (ls.Name != labels[ls.Index].LabelName)
                {
                    return false;
                }
                values[i] = labels[ls.Index].LabelValue;
            }
            return true;
        }

        LabelSelector[] BuildLabelSort(ReadOnlySpan<(string LabelName, string LabelValue)> labels)
        {
            LabelSelector[] sort = new LabelSelector[labels.Length];
            for(int i = 0; i < labels.Length; i++)
            {
                sort[i].Index = i;
                sort[i].Name = labels[i].LabelName;
            }
            Array.Sort(sort, (a, b) => a.Name.CompareTo(b.Name));
            return sort;
        }

        object CreateLabelValuesDictionary(int labelNamesLength)
        {
            if(labelNamesLength == 0)
            {
                return new TAggregator();
            }
            else if(labelNamesLength == 1)
            {
                return new ConcurrentDictionary<string, TAggregator>();
            }
            else if(labelNamesLength == 2)
            {
                return new ConcurrentDictionary<LabelValues2, TAggregator>();
            }
            else if (labelNamesLength == 3)
            {
                return new ConcurrentDictionary<LabelValues3, TAggregator>();
            }
            else
            {
                return new ConcurrentDictionary<LabelValuesMany, TAggregator>();
            }
        }

        LabeledAggregationStatistics MergeStaticLabels(MeterInstrumentBase instrument, LabeledAggregationStatistics aggStats)
        {
            foreach(KeyValuePair<string,string> kv in instrument.StaticLabels)
            {
                foreach((string Name, string Value) label in aggStats.Labels)
                {
                    if(label.Name == kv.Key)
                    {
                        // dynamic label name collided with static name which is not allowed
                        return null;
                    }
                }
            }
            return aggStats.WithLabels(instrument.StaticLabels);
        }

        public override void Collect(MeterInstrumentBase instrument, Action<LabeledAggregationStatistics> visitFunc)
        {
            // post-process labels to merge the static labels onto the dynamic labels we've been tracking
            Action<LabeledAggregationStatistics> visitAndMergeFunc = (aggStats) =>
            {
                LabeledAggregationStatistics mergedLabelStats = MergeStaticLabels(instrument, aggStats);
                if (mergedLabelStats != null)
                {
                    visitFunc(mergedLabelStats);
                }
                else
                {
                    // aggregation has been dropped because the dynamic labels collided with a static label
                    // we should figure out how the SDK wants to log errors because this should probably be one
                }
            };
            CollectUnmerged(instrument, visitAndMergeFunc);
        }

        void CollectUnmerged(MeterInstrumentBase instrument, Action<LabeledAggregationStatistics> visitFunc)
        {
            object cachedNamesAggregations = _cachedNamesAggregations;
            if (cachedNamesAggregations is TAggregator agg)
            {
                visitFunc(new LabeledAggregationStatistics(agg.Collect()));
            }
            else if (cachedNamesAggregations is ConcurrentDictionary<string,TAggregator> aggs1)
            {
                foreach (var kv in aggs1)
                {
                    visitFunc(new LabeledAggregationStatistics(kv.Value.Collect(), (this._cachedLabelNames[0].Name, kv.Key)));
                }
            }
            else if (cachedNamesAggregations is ConcurrentDictionary<LabelValues2, TAggregator> aggs2)
            {
                foreach (var kv in aggs2)
                {
                    visitFunc(new LabeledAggregationStatistics(kv.Value.Collect(),
                          (_cachedLabelNames[0].Name, kv.Key.Value1),
                          (_cachedLabelNames[1].Name, kv.Key.Value2)));
                }
            }
            else if (cachedNamesAggregations is ConcurrentDictionary<LabelValues3, TAggregator> aggs3)
            {
                foreach (var kv in aggs3)
                {
                    visitFunc(new LabeledAggregationStatistics(kv.Value.Collect(),
                          (_cachedLabelNames[0].Name, kv.Key.Value1),
                          (_cachedLabelNames[1].Name, kv.Key.Value2),
                          (_cachedLabelNames[2].Name, kv.Key.Value3)));
                }
            }
            else if (cachedNamesAggregations is ConcurrentDictionary<LabelValuesMany, TAggregator> aggsMany)
            {
                foreach (KeyValuePair<LabelValuesMany,TAggregator> kv in aggsMany)
                {
                    visitFunc(new LabeledAggregationStatistics(kv.Value.Collect(),
                        kv.Key.Values.Select((v, i) => (_cachedLabelNames[i].Name, v))));
                }
            }
            _uncachedNamesAggregations.Collect(visitFunc);
        }
    }

    struct LabelSelector
    {
        public string Name;
        public int Index;
    }

    struct StackStringBuffer
    {
        public string Value1;
        public string Value2;
        public string Value3;
        public const int Size = 3;
    }

    struct LabelValues2 : IEquatable<LabelValues2>
    {
        public string Value1;
        public string Value2;

        public LabelValues2(string value1, string value2)
        {
            Value1 = value1;
            Value2 = value2;
        }

        public override int GetHashCode() => HashCode.Combine(Value1.GetHashCode(), Value2.GetHashCode());

        public bool Equals(LabelValues2 other)
        {
            return Value1 == other.Value1 && Value2 == other.Value2;
        }

        public override bool Equals(object obj)
        {
            return obj is LabelValues2 && Equals((LabelValues2)obj);
        }
    }

    struct LabelValues3 : IEquatable<LabelValues3>
    {
        public string Value1;
        public string Value2;
        public string Value3;

        public LabelValues3(string value1, string value2, string value3)
        {
            Value1 = value1;
            Value2 = value2;
            Value3 = value3;
        }

        public override int GetHashCode() => HashCode.Combine(Value1.GetHashCode(), Value2.GetHashCode(), Value3.GetHashCode());

        public bool Equals(LabelValues3 other)
        {
            return Value1 == other.Value1 && Value2 == other.Value2 && Value3 == other.Value3;
        }

        public override bool Equals(object obj)
        {
            return obj is LabelValues3 && Equals((LabelValues3)obj);
        }
    }

    struct LabelValuesMany : IEquatable<LabelValuesMany>
    {
        string[] _labelValues;

        public LabelValuesMany(string[] values)
        {
            _labelValues = values;
        }

        public IEnumerable<string> Values => _labelValues;

        public override int GetHashCode()
        {
            int hash = 0;
            for (int i = 0; i < _labelValues.Length; i++)
            {
                hash = (int)BitOperations.RotateLeft((uint)hash, 3);
                hash ^= _labelValues[i].GetHashCode();
            }
            return hash;
        }

        public bool Equals(LabelValuesMany other)
        {
            return _labelValues.SequenceEqual(other._labelValues);
        }

        public override bool Equals(object obj)
        {
            return obj is LabelValuesMany && Equals((LabelValuesMany)obj);
        }
    }
}
