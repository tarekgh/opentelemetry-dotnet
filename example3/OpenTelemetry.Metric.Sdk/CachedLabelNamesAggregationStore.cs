using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading;
using Microsoft.Diagnostics.Metric;

namespace OpenTelemetry.Metric.Sdk
{
    /// <summary>
    /// This store caches the first set of label names that are observed in Update. All further label sets that have
    /// those same label names will be stored in the cache. Label sets that have additional/fewer/different label names
    /// will be passed through to the MultiSizeAggregationStore.
    /// Label sets that exactly match the sequence of the original names should be fast, sets that have the same names
    /// but in a different order will need to be resorted which might be much slower.
    ///
    /// This class doesn't handle adding/removing/modifying labels as they pass through but I assume we could create
    /// a modified implementation that did do that. 
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
                UpdateManyLabels(measurement, labels);
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

        void UpdateManyLabels(double measurement, ReadOnlySpan<(string LabelName, string LabelValue)> labels)
        {
            if (_cachedLabelNames.Length != labels.Length)
            {
                _uncachedNamesAggregations.UpdateManyLabelsUnsorted(measurement, labels);
                return;
            }

            LabelSelector[] cachedLabelNames = _cachedLabelNames;
            string[] sortedLabelValues = GetSortedLabelValues(cachedLabelNames, labels);
            if(sortedLabelValues == null)
            {
                // is it the same names but re-ordered? This can have slow path performance but regardless
                // of order any labelset that matches the cached names must get aggregated in the cachedNames
                // dictionary for correctness.
                LabelSelector[] labelSort = BuildLabelSort(labels);
                int i = 0;
                for(; i < cachedLabelNames.Length; i++)
                {
                    if(labelSort[i].Name != cachedLabelNames[i].Name)
                    {
                        break;
                    }
                }
                if(i != cachedLabelNames.Length)
                {
                    _uncachedNamesAggregations.UpdateManyLabelsUnsorted(measurement, labels);
                    return;
                }
                sortedLabelValues = GetSortedLabelValues(labelSort, labels);
            }
            Debug.Assert(sortedLabelValues != null);

            LabelValuesMany key = new LabelValuesMany(sortedLabelValues);
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
        /// a sorted array. If the label names don't match the sort then null is returned.
        /// </summary>
        string[] GetSortedLabelValues(LabelSelector[] sort, ReadOnlySpan<(string LabelName, string LabelValue)> labels)
        {
            for (int i = 0; i < sort.Length; i++)
            {
                if (sort[i].Name != labels[sort[i].Index].LabelName)
                {
                    return null;
                }
            }
            string[] ret = new string[sort.Length];
            for (int i = 0; i < sort.Length; i++)
            {
                ret[i] = labels[sort[i].Index].LabelValue;
            }
            return ret;
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
