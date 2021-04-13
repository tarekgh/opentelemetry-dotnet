using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Running;

using Microsoft.Diagnostics.Metric;
using OpenTelemetry.Metric.Sdk;

namespace MyBenchmark
{
    [SimpleJob(launchCount: 2, warmupCount: 2, targetCount: 10)]
    [MemoryDiagnoser]
    public class LabelSetBench
    {
        /*
            BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19042
            Intel Core i7-1065G7 CPU 1.30GHz, 1 CPU, 8 logical and 4 physical cores
            .NET Core SDK=5.0.102
            [Host]     : .NET Core 5.0.2 (CoreCLR 5.0.220.61120, CoreFX 5.0.220.61120), X64 RyuJIT
            Job-UEMYAQ : .NET Core 5.0.2 (CoreCLR 5.0.220.61120, CoreFX 5.0.220.61120), X64 RyuJIT

            IterationCount=10  LaunchCount=2  WarmupCount=2

            |              Method |        Mean |      Error |     StdDev |  Gen 0 | Gen 1 | Gen 2 | Allocated |
            |-------------------- |------------:|-----------:|-----------:|-------:|------:|------:|----------:|
            |            Dict_new | 248.0862 ns | 18.6159 ns | 19.9188 ns | 0.2370 |     - |     - |     992 B |
            |           Dict_enum | 261.0479 ns | 10.1366 ns | 10.8461 ns | 0.1602 |     - |     - |     672 B |
            |     ValueTuple_enum | 256.9502 ns | 21.3555 ns | 24.5930 ns | 0.1450 |     - |     - |     608 B |
            |        LabelSet_new |  52.0137 ns |  2.9141 ns |  3.2390 ns | 0.0516 |     - |     - |     216 B |
            |       LabelSet_enum | 128.5064 ns |  4.7004 ns |  5.0294 ns | 0.1318 |     - |     - |     552 B |
            |  LabelSet_enumArray |   0.5445 ns |  0.0719 ns |  0.0828 ns |      - |     - |     - |         - |
        */

        MetricLabelSet ls;

        IDictionary<string, string> dict;

        [GlobalSetup]
        public void Setup()
        {
            ls = new MetricLabelSet(
                ("Key1", "Value1"),
                ("Key2", "Value2"),
                ("Key3", "Value3"),
                ("Key4", "Value4"),
                ("Key5", "Value5"),
                ("Key6", "Value6"),
                ("Key7", "Value7"),
                ("Key8", "Value8"),
                ("Key9", "Value9")
                );

            dict = new Dictionary<string, string> {
                { "Key1", "Value1" },
                { "Key2", "Value2" },
                { "Key3", "Value3" },
                { "Key4", "Value4" },
                { "Key5", "Value5" },
                { "Key6", "Value6" },
                { "Key7", "Value7" },
                { "Key8", "Value8" },
                { "Key9", "Value9" },
            };
        }

        //****************

        [Benchmark]
        public IDictionary<string, string> Dict_new()
        {
            return new Dictionary<string, string> {
                { "Key1", "Value1" },
                { "Key2", "Value2" },
                { "Key3", "Value3" },
                { "Key4", "Value4" },
                { "Key5", "Value5" },
                { "Key6", "Value6" },
                { "Key7", "Value7" },
                { "Key8", "Value8" },
                { "Key9", "Value9" },
            };
        }

        [Benchmark]
        public List<Tuple<string, string>> Dict_enum()
        {
            List<Tuple<string, string>> ret = new();

            foreach (var kv in dict)
            {
                var key = kv.Key;
                var val = kv.Value;

                ret.Add(Tuple.Create(key, val));
            }

            return ret;
        }

        [Benchmark]
        public List<(string, string)> ValueTuple_enum()
        {
            List<(string, string)> ret = new();

            foreach (var kv in dict)
            {
                var key = kv.Key;
                var val = kv.Value;

                ret.Add((key, val));
            }

            return ret;
        }

        //****************

        [Benchmark]
        public MetricLabelSet MetricLabelSet_new()
        {
            return new MetricLabelSet(
                ("Key1", "Value1"),
                ("Key2", "Value2"),
                ("Key3", "Value3"),
                ("Key4", "Value4"),
                ("Key5", "Value5"),
                ("Key6", "Value6"),
                ("Key7", "Value7"),
                ("Key8", "Value8"),
                ("Key9", "Value9")
                );
        }

        [Benchmark]
        public List<(string, string)> MetricLabelSet_enum()
        {
            List<(string, string)> ret = new();

            var ls = this.ls.GetLabels();
            foreach (var label in ls)
            {
                ret.Add((label.name, label.value));
            }

            return ret;
        }

        [Benchmark]
        public (string name, string value)[] MetricLabelSet_enumArray()
        {
            return ls.GetLabels();
        }
    }
}
