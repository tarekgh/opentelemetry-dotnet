using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Metric;

namespace OpenTelemetry.Metric.Sdk
{
    class AggregatorStore<TAggregator> where TAggregator : Aggregator, new()
    {
        // this union can be:
        // null
        // TAggregator
        // FixedSizeLabelNameDictionary<StringSequence1, TAggregator>
        // FixedSizeLabelNameDictionary<StringSequence2, TAggregator>
        // ...
        // FixedSizeLabelNameDictionary<StringSequenceMany, TAggregator>
        // MultiSizeLabelNameDictionary<TAggregator>
        volatile object _stateUnion;
        volatile AggregatorLookupFunc<TAggregator> _cachedLookupFunc;
        LabelProcessingConfiguration _labelConfig;

        public AggregatorStore(LabelProcessingConfiguration labelConfig)
        {
            _labelConfig = labelConfig;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public TAggregator GetAggregator(ReadOnlySpan<(string LabelName, string LabelValue)> labels)
        {
            TAggregator aggregator = null;
            AggregatorLookupFunc<TAggregator> lookupFunc = _cachedLookupFunc;
            if(lookupFunc != null)
            {
                if (lookupFunc(labels, ref aggregator)) return aggregator;
            }

            // slow path, label names have changed from what the lookupFunc cached so we need to
            // rebuild it
            return GetAggregatorSlow(labels);
        }

        public TAggregator GetAggregatorSlow(ReadOnlySpan<(string LabelName, string LabelValue)> labels)
        {
            TAggregator aggregator = null;
            AggregatorLookupFunc<TAggregator> lookupFunc = LabelInstructionCompiler.Create(this, _labelConfig, labels, LogErrors);
            _cachedLookupFunc = lookupFunc;
            bool match = lookupFunc(labels, ref aggregator);
            Debug.Assert(match);
            return aggregator;
        }

        static void LogErrors(List<string> errors)
        {
            //TODO: how do we want to log errors?
        }


        public void Collect(Action<LabeledAggregationStatistics> visitFunc)
        {
            object stateUnion = _stateUnion;
            if (stateUnion is TAggregator agg)
            {
                visitFunc(new LabeledAggregationStatistics(agg.Collect()));
            }
            else if (stateUnion is FixedSizeLabelNameDictionary<StringSequence1, TAggregator> aggs1)
            {
                aggs1.Collect(visitFunc);
            }
            else if (stateUnion is FixedSizeLabelNameDictionary<StringSequence2, TAggregator> aggs2)
            {
                aggs2.Collect(visitFunc);
            }
            else if (stateUnion is FixedSizeLabelNameDictionary<StringSequence3, TAggregator> aggs3)
            {
                aggs3.Collect(visitFunc);
            }
            else if (stateUnion is FixedSizeLabelNameDictionary<StringSequenceMany, TAggregator> aggsMany)
            {
                aggsMany.Collect(visitFunc);
            }
            else if (stateUnion is MultiSizeLabelNameDictionary<TAggregator> aggsMultiSize)
            {
                aggsMultiSize.Collect(visitFunc);
            }
        }

        public TAggregator GetAggregator()
        {
            while (true)
            {
                object state = _stateUnion;
                if (state == null)
                {
                    var newState = new TAggregator();
                    Interlocked.CompareExchange(ref _stateUnion, newState, null);
                    continue;
                }
                else if (state is TAggregator aggState)
                {
                    return aggState;
                }
                else if (state is MultiSizeLabelNameDictionary<TAggregator> multiSizeState)
                {
                    return multiSizeState.GetNoLabelAggregator();
                }
                else
                {
                    var newState = new MultiSizeLabelNameDictionary<TAggregator>(state);
                    Interlocked.CompareExchange(ref _stateUnion, newState, state);
                    continue;
                }
            }
        }

        public ConcurrentDictionary<TStringSequence,TAggregator> GetLabelValuesDictionary<TStringSequence>(in TStringSequence names)
            where TStringSequence : IStringSequence, IEquatable<TStringSequence>
        {
            while(true)
            {
                object state = _stateUnion;
                if (state == null)
                {
                    var newState = new FixedSizeLabelNameDictionary<TStringSequence, TAggregator>();
                    Interlocked.CompareExchange(ref _stateUnion, newState, null);
                    continue;
                }
                else if (state is FixedSizeLabelNameDictionary<TStringSequence, TAggregator> fixedState)
                {
                    return fixedState.GetValuesDictionary(names);
                }
                else if(state is MultiSizeLabelNameDictionary<TAggregator> multiSizeState)
                {
                    return multiSizeState.GetFixedSizeLabelNameDictionary<TStringSequence>().GetValuesDictionary(names);
                }
                else
                {
                    var newState = new MultiSizeLabelNameDictionary<TAggregator>(state);
                    Interlocked.CompareExchange(ref _stateUnion, newState, state);
                    continue;
                }
            }
        }
    }

    class MultiSizeLabelNameDictionary<TAggregator> where TAggregator : Aggregator, new()
    {
        TAggregator NoLabelAggregator;
        FixedSizeLabelNameDictionary<StringSequence1, TAggregator> Label1;
        FixedSizeLabelNameDictionary<StringSequence2, TAggregator> Label2;
        FixedSizeLabelNameDictionary<StringSequence3, TAggregator> Label3;
        FixedSizeLabelNameDictionary<StringSequenceMany, TAggregator> LabelMany;

        public MultiSizeLabelNameDictionary(object initialLabelNameDict)
        {
            if (initialLabelNameDict is TAggregator val0)
            {
                NoLabelAggregator = val0;
            }
            else if (initialLabelNameDict is FixedSizeLabelNameDictionary<StringSequence1, TAggregator> val1)
            {
                Label1 = val1;
            }
            else if (initialLabelNameDict is FixedSizeLabelNameDictionary<StringSequence2, TAggregator> val2)
            {
                Label2 = val2;
            }
            else if (initialLabelNameDict is FixedSizeLabelNameDictionary<StringSequence3, TAggregator> val3)
            {
                Label3 = val3;
            }
            else if (initialLabelNameDict is FixedSizeLabelNameDictionary<StringSequenceMany, TAggregator> valMany)
            {
                LabelMany = valMany;
            }
        }

        public TAggregator GetNoLabelAggregator()
        {
            if (NoLabelAggregator == null)
            {
                Interlocked.CompareExchange(ref NoLabelAggregator, new TAggregator(), null);
            }
            return NoLabelAggregator;
        }

        public FixedSizeLabelNameDictionary<TStringSequence,TAggregator> GetFixedSizeLabelNameDictionary<TStringSequence>()
            where TStringSequence : IStringSequence, IEquatable<TStringSequence>
        {
            TStringSequence seq = default;
            if(seq is StringSequence1)
            {
                if(Label1 == null)
                {
                    Interlocked.CompareExchange(ref Label1, new FixedSizeLabelNameDictionary<StringSequence1, TAggregator>(), null);
                }
                return (FixedSizeLabelNameDictionary<TStringSequence, TAggregator>)(object)Label1;
            }
            else if (seq is StringSequence2)
            {
                if (Label2 == null)
                {
                    Interlocked.CompareExchange(ref Label2, new FixedSizeLabelNameDictionary<StringSequence2, TAggregator>(), null);
                }
                return (FixedSizeLabelNameDictionary<TStringSequence, TAggregator>)(object)Label2;
            }
            else if (seq is StringSequence3)
            {
                if (Label3 == null)
                {
                    Interlocked.CompareExchange(ref Label3, new FixedSizeLabelNameDictionary<StringSequence3, TAggregator>(), null);
                }
                return (FixedSizeLabelNameDictionary<TStringSequence, TAggregator>)(object)Label3;
            }
            else if (seq is StringSequenceMany)
            {
                if (LabelMany == null)
                {
                    Interlocked.CompareExchange(ref LabelMany, new FixedSizeLabelNameDictionary<StringSequenceMany, TAggregator>(), null);
                }
                return (FixedSizeLabelNameDictionary<TStringSequence, TAggregator>)(object)LabelMany;
            }
            return null;
        }

        public void Collect(Action<LabeledAggregationStatistics> visitFunc)
        {
            if (NoLabelAggregator != null)
            {
                visitFunc(new LabeledAggregationStatistics(NoLabelAggregator.Collect()));
            }
            Label1?.Collect(visitFunc);
            Label2?.Collect(visitFunc);
            Label3?.Collect(visitFunc);
            LabelMany?.Collect(visitFunc);
        }
    }


    class LabelProcessingConfiguration
    {
        public bool IncludeAllCallsiteLabels;
        public Func<string, string> DefaultCallsiteLabelTransform;
        public LabelConfiguration[] FixedNameLabels = Array.Empty<LabelConfiguration>();
    }

    class LabelConfiguration
    {
        public string LabelName;
        public bool RequiresSourceLabel;
        public Func<string, string> ComputeLabelValue;
    }


    struct LabelCompilation
    {
        public LabelInstruction[] Instructions;
        public List<string> Errors;

        public void AddError(string errorMessage)
        {
            if(Errors == null)
            {
                Errors = new List<string>();
            }
            Errors.Add(errorMessage);
        }
    }

    struct LabelInstruction
    {
        public int SourceIndex;
        public string LabelName;
        public Func<string, string> ComputeLabelValue;
    }

    delegate bool AggregatorLookupFunc<TAggregator>(ReadOnlySpan<(string LabelName, string LabelValue)> labels, ref TAggregator aggregator);

    static class LabelInstructionCompiler
    {
        public static AggregatorLookupFunc<TAggregator> Create<TAggregator>(
            AggregatorStore<TAggregator> aggregatorStore,
            LabelProcessingConfiguration processingConfig,
            ReadOnlySpan<(string LabelName, string LabelValue)> labels,
            Action<List<string>> errorLogger)
            where TAggregator : Aggregator, new()
        {
            LabelCompilation compilation;
            if (!processingConfig.IncludeAllCallsiteLabels)
            {
                compilation = CompileFixedLabels(processingConfig, labels);
            }
            else
            {
                compilation = CompileVariableLabels(processingConfig, labels);
            }

            if(compilation.Errors != null)
            {
                return CreateErrorLoggingAggregatorLookup<TAggregator>(labels, compilation.Errors, errorLogger);
            }

            LabelInstruction[] instructions = compilation.Instructions;
            Array.Sort(instructions, (LabelInstruction a, LabelInstruction b) => a.LabelName.CompareTo(b.LabelName));
            int expectedLabels = labels.Length;
            switch (instructions.Length)
            {
                case 0:
                    return (ReadOnlySpan<(string LabelName, string LabelValue)> l, ref TAggregator aggregator) =>
                    {
                        if (l.Length != expectedLabels) return false;
                        aggregator = aggregatorStore.GetAggregator();
                        return true;
                    };
                        
                case 1:
                    StringSequence1 names1 = new StringSequence1(instructions[0].LabelName);
                    ConcurrentDictionary<StringSequence1, TAggregator> valuesDict1 = aggregatorStore.GetLabelValuesDictionary(names1);
                    return (ReadOnlySpan<(string LabelName, string LabelValue)> l, ref TAggregator aggregator) =>
                        LabelInstructionInterpretter.GetAggregator(expectedLabels, instructions, valuesDict1, l, ref aggregator);
                case 2:
                    StringSequence2 names2 = new StringSequence2(instructions[0].LabelName, instructions[1].LabelName);
                    ConcurrentDictionary<StringSequence2, TAggregator> valuesDict2 = aggregatorStore.GetLabelValuesDictionary(names2);
                    return (ReadOnlySpan<(string LabelName, string LabelValue)> l, ref TAggregator aggregator) =>
                        LabelInstructionInterpretter.GetAggregator(expectedLabels, instructions, valuesDict2, l, ref aggregator);
                case 3:
                    StringSequence3 names3 = new StringSequence3(instructions[0].LabelName, instructions[1].LabelName,
                        instructions[2].LabelName);
                    ConcurrentDictionary<StringSequence3, TAggregator> valuesDict3 = aggregatorStore.GetLabelValuesDictionary(names3);
                    return (ReadOnlySpan<(string LabelName, string LabelValue)> l, ref TAggregator aggregator) =>
                        LabelInstructionInterpretter.GetAggregator(expectedLabels, instructions, valuesDict3, l, ref aggregator);
                default:
                    StringSequenceMany namesMany = new StringSequenceMany(instructions.Select(instr => instr.LabelName).ToArray());
                    ConcurrentDictionary<StringSequenceMany, TAggregator> valuesDictMany = aggregatorStore.GetLabelValuesDictionary(namesMany);
                    return (ReadOnlySpan<(string LabelName, string LabelValue)> l, ref TAggregator aggregator) =>
                        LabelInstructionInterpretter.GetAggregator(expectedLabels, instructions, valuesDictMany, l, ref aggregator);
            }
        }

        private static LabelCompilation CompileVariableLabels(LabelProcessingConfiguration processingConfig, ReadOnlySpan<(string LabelName, string LabelValue)> labels)
        {
            LabelCompilation compilation = new LabelCompilation();
            LabelInstruction[] valueComputations;
            {
                int fixedNames = 0;
                for (int i = 0; i < processingConfig.FixedNameLabels.Length; i++)
                {
                    int j = 0;
                    string knownName = processingConfig.FixedNameLabels[i].LabelName;
                    for (; j < labels.Length; j++)
                    {
                        if (knownName == labels[j].LabelName)
                        {
                            break;
                        }
                    }
                    if (j == labels.Length)
                    {
                        fixedNames++;
                    }
                }

                valueComputations = new LabelInstruction[labels.Length + fixedNames];
                for (int i = 0; i < labels.Length; i++)
                {
                    valueComputations[i].LabelName = labels[i].LabelName;
                    valueComputations[i].SourceIndex = i;
                    valueComputations[i].ComputeLabelValue = processingConfig.DefaultCallsiteLabelTransform;
                }

                int curFixedName = 0;
                for (int i = 0; i < processingConfig.FixedNameLabels.Length; i++)
                {
                    LabelConfiguration lc = processingConfig.FixedNameLabels[i];
                    Debug.Assert(lc.RequiresSourceLabel || lc.ComputeLabelValue != null);
                    string knownName = lc.LabelName;
                    int vcIndex = -1;
                    for (int j = 0; j < labels.Length; j++)
                    {
                        if (knownName == labels[j].LabelName)
                        {
                            vcIndex = j;
                            break;
                        }
                    }
                    if (vcIndex == -1) // didn't find a label with this name
                    {
                        vcIndex = labels.Length + curFixedName;
                        curFixedName++;
                        Debug.Assert(valueComputations[vcIndex].LabelName == null);
                        valueComputations[vcIndex].LabelName = lc.LabelName;
                        valueComputations[vcIndex].SourceIndex = -1;
                        if (lc.RequiresSourceLabel)
                        {
                            compilation.AddError($"Label {lc.LabelName} not present");
                        }
                        else
                        {
                            valueComputations[vcIndex].ComputeLabelValue = lc.ComputeLabelValue;
                        }
                    }
                    else // found a label with this name
                    {
                        if (!lc.RequiresSourceLabel)
                        {
                            valueComputations[vcIndex].SourceIndex = -1;
                        }
                        valueComputations[vcIndex].ComputeLabelValue = lc.ComputeLabelValue;
                    }
                }
                Debug.Assert(curFixedName == fixedNames);
            }

            compilation.Instructions = valueComputations;
            return compilation;
        }

        private static LabelCompilation CompileFixedLabels(LabelProcessingConfiguration processingConfig, ReadOnlySpan<(string LabelName, string LabelValue)> labels)
        {
            LabelCompilation compilation = new LabelCompilation();
            LabelInstruction[] valueComputations;
            int fixedNames = processingConfig.FixedNameLabels.Length;
            valueComputations = new LabelInstruction[fixedNames];
            for (int i = 0; i < fixedNames; i++)
            {
                LabelConfiguration lc = processingConfig.FixedNameLabels[i];
                Debug.Assert(lc.RequiresSourceLabel || lc.ComputeLabelValue != null);
                valueComputations[i].LabelName = lc.LabelName;
                valueComputations[i].ComputeLabelValue = lc.ComputeLabelValue;
                int sourceIndex = -1;
                if (lc.RequiresSourceLabel)
                {
                    string knownName = lc.LabelName;
                    for (int j = 0; j < labels.Length; j++)
                    {
                        if (knownName == labels[j].LabelName)
                        {
                            sourceIndex = j;
                            break;
                        }
                    }
                }
                valueComputations[i].SourceIndex = sourceIndex;
                if (lc.RequiresSourceLabel && sourceIndex == -1)
                {
                    compilation.AddError($"Label {lc.LabelName} not present");
                }
            }

            compilation.Instructions = valueComputations;
            return compilation;
        }


        private static AggregatorLookupFunc<TAggregator> CreateErrorLoggingAggregatorLookup<TAggregator>(
            ReadOnlySpan<(string LabelName, string LabelValue)> labels,
            List<string> errors,
            Action<List<string>> errorLogger)
            where TAggregator : Aggregator, new()
        {

            string[] expectedLabelNames = new string[labels.Length];
            for(int i = 0; i < labels.Length; i++)
            {
                expectedLabelNames[i] = labels[i].LabelName;
            }
            return (ReadOnlySpan<(string LabelName, string LabelValue)> l, ref TAggregator aggregator) =>
            {
                if (expectedLabelNames.Length != l.Length)
                {
                    return false;
                }
                for (int i = 0; i < expectedLabelNames.Length; i++)
                {
                    if (l[i].LabelName != expectedLabelNames[i])
                    {
                        return false;
                    }
                }
                errorLogger(errors);
                return true;
            };
        }
    }

    class LabelInstructionInterpretter
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static bool GetAggregator<TStringSequence, TAggregator>(
            int expectedLabelCount,
            LabelInstruction[] instructions,
            ConcurrentDictionary<TStringSequence, TAggregator> valuesDict,
            ReadOnlySpan<(string labelName, string labelValue)> labels,
            ref TAggregator aggregator)
            where TStringSequence : IStringSequence, IEquatable<TStringSequence>
            where TAggregator : Aggregator, new()
        {
            if(labels.Length != expectedLabelCount)
            {
                return false;
            }

            TStringSequence values = default;
            if(values is StringSequenceMany)
            {
                values = (TStringSequence)(object)new StringSequenceMany(new string[expectedLabelCount]);
            }
            Span<string> valuesSpan = values.AsSpan();

            for(int i = 0; i < instructions.Length; i++)
            {
                LabelInstruction instr = instructions[i];

                if (instr.ComputeLabelValue != null)
                {
                    string inputValue = null;
                    if (instr.SourceIndex != -1)
                    {
                        if(instr.LabelName != labels[instr.SourceIndex].labelName)
                        {
                            return false;
                        }
                        inputValue = labels[instr.SourceIndex].labelValue;
                    }
                    valuesSpan[i] = instr.ComputeLabelValue(inputValue);
                }
                else
                {
                    if (instr.LabelName != labels[instr.SourceIndex].labelName)
                    {
                        return false;
                    }
                    valuesSpan[i] = labels[instr.SourceIndex].labelValue;
                }
            }

            aggregator = valuesDict.GetOrAdd(values, v => new TAggregator());
            return true;
        }
    }

    class FixedSizeLabelNameDictionary<TStringSequence, TAggregator> :
        ConcurrentDictionary<TStringSequence, ConcurrentDictionary<TStringSequence, TAggregator>>
        where TAggregator : Aggregator, new()
        where TStringSequence : IStringSequence, IEquatable<TStringSequence>
    {
        public void Collect(Action<LabeledAggregationStatistics> visitFunc)
        {
            foreach(KeyValuePair<TStringSequence, ConcurrentDictionary<TStringSequence, TAggregator>> kvName in this)
            {
                string[] names = kvName.Key.AsSpan().ToArray();
                foreach(KeyValuePair<TStringSequence,TAggregator> kvValue in kvName.Value)
                {
                    Span<string> values = kvValue.Key.AsSpan();
                    var labels = new (string LabelName, string LabelValue)[names.Length];
                    for(int i = 0; i < labels.Length; i++)
                    {
                        labels[i] = (names[i], values[i]);
                    }
                    visitFunc(new LabeledAggregationStatistics(kvValue.Value.Collect(), labels));
                }
            }
        }

        public ConcurrentDictionary<TStringSequence, TAggregator> GetValuesDictionary(in TStringSequence names) =>
            GetOrAdd(names, _ => new ConcurrentDictionary<TStringSequence, TAggregator>());
    }
}
