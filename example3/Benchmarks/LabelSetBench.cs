using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Engines;

using OpenTelemetry.Metric.Api;
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
            |  LabelSetSplit_new1 |  86.8414 ns |  1.2100 ns |  1.3450 ns | 0.0937 |     - |     - |     392 B |
            |  LabelSetSplit_new2 |  56.7981 ns |  0.7631 ns |  0.8165 ns | 0.0535 |     - |     - |     224 B |
            |  LabelSetSplit_enum |  92.8131 ns |  5.1356 ns |  5.9142 ns | 0.0918 |     - |     - |     384 B |
            | LabelSetSplit_enum1 | 131.5840 ns | 13.1180 ns | 13.4712 ns | 0.1318 |     - |     - |     552 B |
        */

        LabelSet ls;
        LabelSetSplit split;

        IDictionary<string,string> dict;

        [GlobalSetup]
        public void Setup()
        {
            ls = new LabelSet(
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

            dict = new Dictionary<string,string> {
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

            split = new LabelSetSplit(
                "Key1", "Value1",
                "Key2", "Value2",
                "Key3", "Value3",
                "Key4", "Value4",
                "Key5", "Value5",
                "Key6", "Value6",
                "Key7", "Value7",
                "Key8", "Value8",
                "Key9", "Value9"
                );
        }

        //****************

        [Benchmark]
        public IDictionary<string,string> Dict_new()
        {
            return new Dictionary<string,string> {
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
        public List<Tuple<string,string>> Dict_enum()
        {
            List<Tuple<string,string>> ret = new();

            foreach(var kv in dict)
            {
                var key = kv.Key;
                var val = kv.Value;

                ret.Add(Tuple.Create(key,val));
            }

            return ret;
        }

        [Benchmark]
        public List<(string,string)> ValueTuple_enum()
        {
            List<(string,string)> ret = new();

            foreach(var kv in dict)
            {
                var key = kv.Key;
                var val = kv.Value;

                ret.Add((key,val));
            }

            return ret;
        }

        //****************

        [Benchmark]
        public LabelSet LabelSet_new()
        {
            return new LabelSet(
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
        public List<(string,string)> LabelSet_enum()
        {
            List<(string,string)> ret = new();

            var ls = this.ls.GetLabels();
            foreach (var label in ls)
            {
                ret.Add((label.name, label.value));
            }

            return ret;
        }

        [Benchmark]
        public (string name, string value)[] LabelSet_enumArray()
        {
            return ls.GetLabels();
        }

        //****************

        [Benchmark]
        public LabelSetSplit LabelSetSplit_new1()
        {
            return new LabelSetSplit(
                "Key1", "Value1",
                "Key2", "Value2",
                "Key3", "Value3",
                "Key4", "Value4",
                "Key5", "Value5",
                "Key6", "Value6",
                "Key7", "Value7",
                "Key8", "Value8",
                "Key9", "Value9"
                );
        }

        [Benchmark]
        public LabelSetSplit LabelSetSplit_new2()
        {
            return new LabelSetSplit(
                new string[] {
                    "Key1", "Key2", "Key3", "Key4", "Key5", "Key6", "Key7", "Key8", "Key9"
                    },
                new string[] {
                    "Value1", "Value2", "Value3", "Value4", "Value5", "Value6", "Value7", "Value8", "Value9"
                    }
            );
        }

        [Benchmark]
        public Tuple<string,string>[] LabelSetSplit_enum()
        {
            return split.GetLabels();
        }

        [Benchmark]
        public Tuple<string,string>[] LabelSetSplit_enum1()
        {
            var ls = split.GetKeyValues();

            var ret = new Tuple<string,string>[ls.Length / 2];

            int pos = 0;
            for (int n = 0; n < ls.Length; n += 2)
            {
                var key = ls[n];
                var val = ls[n+1];
                ret[pos++] = Tuple.Create(key,val);
            }

            return ret;
        }

        //****************
    }
}