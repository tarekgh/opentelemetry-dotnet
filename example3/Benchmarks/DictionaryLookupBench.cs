using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using Microsoft.Diagnostics.Metric;
using OpenTelemetry.Metric.Api;
using OpenTelemetry.Metric.Sdk;

namespace Benchmarks
{
    [EtwProfiler]
    [MemoryDiagnoser]
    public class DictionaryLookupBench
    {
        static Dictionary<ValueTuple<string, string>, object> _tupleDictionary = new Dictionary<(string, string), object>();
        static ConcurrentDictionary<ValueTuple<string, string>, object> _tupleConcurrentDictionary = new ConcurrentDictionary<(string, string), object>();
        static Dictionary<TwoStrings, object> _twoStrDictionary = new Dictionary<TwoStrings, object>();
        static ConcurrentDictionary<TwoStrings, object> _twoStrConcurrentDictionary = new ConcurrentDictionary<TwoStrings, object>();


        static DictionaryLookupBench()
        {
            _tupleDictionary.Add(("Red", "Blue"), new object());
            _tupleConcurrentDictionary.TryAdd(("Red", "Blue"), new object());
            _twoStrDictionary.Add(new TwoStrings("Red", "Blue"), new object());
            _twoStrConcurrentDictionary.TryAdd(new TwoStrings("Red", "Blue"), new object());
        }

        [Benchmark]
        public void TupleDict()
        {
            _tupleDictionary.TryGetValue(("Red", "Blue"), out object val);
        }

        [Benchmark]
        public void TupleConcurrentDict()
        {
            _tupleConcurrentDictionary.TryGetValue(("Red", "Blue"), out object val);
        }

        [Benchmark]
        public void TwoStrDict()
        {
            _twoStrDictionary.TryGetValue(new TwoStrings("Red", "Blue"), out object val);
        }

        [Benchmark]
        public void TwoStrConcurrentDict()
        {
            _twoStrConcurrentDictionary.TryGetValue(new TwoStrings("Red", "Blue"), out object val);
        }

    }

    struct TwoStrings : IEquatable<TwoStrings>
    {
        public string One;
        public string Two;

        public TwoStrings(string one, string two)
        {
            One = one;
            Two = two;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(One.GetHashCode(), Two.GetHashCode());
        }

        public override bool Equals(object obj)
        {
            return (obj is TwoStrings) && Equals((TwoStrings)obj);
        }

        public bool Equals(TwoStrings other)
        {
            return One == other.One && Two == other.Two;
        }
    }
}
