using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Engines;

using Microsoft.Diagnostics.Metric;
using Microsoft.OpenTelemetry.Export;
using OpenTelemetry.Metric.Api;
using OpenTelemetry.Metric.Sdk;

/*
BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19042
Intel Core i7-1065G7 CPU 1.30GHz, 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=5.0.201
  [Host]     : .NET Core 5.0.4 (CoreCLR 5.0.421.11614, CoreFX 5.0.421.11614), X64 RyuJIT
  Job-ZJXXBO : .NET Core 5.0.4 (CoreCLR 5.0.421.11614, CoreFX 5.0.421.11614), X64 RyuJIT

IterationCount=20  LaunchCount=5  WarmupCount=5

|                Method |        Mean |     Error |    StdDev |      Median |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|---------------------- |------------:|----------:|----------:|------------:|-------:|------:|------:|----------:|
|           AddByValues |    53.12 ns |  0.389 ns |  1.136 ns |    53.32 ns | 0.0114 |     - |     - |      48 B |
|       AddByValueTuple |    62.24 ns |  1.043 ns |  3.060 ns |    61.39 ns | 0.0172 |     - |     - |      72 B |
|      Add2ByValueTuple |    60.38 ns |  0.531 ns |  1.567 ns |    60.48 ns | 0.0172 |     - |     - |      72 B |
|       AddByDictionary |   170.29 ns |  1.154 ns |  3.384 ns |   170.60 ns | 0.0648 |     - |     - |     272 B |
|      AddByLargeValues |   343.54 ns |  2.424 ns |  7.146 ns |   343.27 ns | 0.0439 |     - |     - |     184 B |
|  AddByLargeValueTuple |   380.91 ns |  2.660 ns |  7.633 ns |   379.75 ns | 0.0820 |     - |     - |     344 B |
| Add2ByLargeValueTuple |   380.08 ns |  2.958 ns |  8.676 ns |   380.62 ns | 0.0820 |     - |     - |     344 B |
|  AddByLargeDictionary | 1,161.07 ns | 11.926 ns | 34.409 ns | 1,162.90 ns | 0.5093 |     - |     - |    2136 B |
*/

namespace MyBenchmark
{
    [SimpleJob(launchCount: 5, warmupCount: 5, targetCount: 20)]
    [MemoryDiagnoser]
    public class LabelPassingBench
    {
        private Counter counter1;
        private Counter counter2;

        [GlobalSetup]
        public void Setup()
        {
            this.counter1 = new Counter(new string[] { "host", "location", "tenantid" });

            this.counter2 = new Counter(new string[] { 
                "col01", "col02", "col03", "col04", "col05", "col06", "col07", "col08", "col09", "col10",
                "col11", "col12", "col13", "col14", "col15", "col16", "col17", "col18", "col19", "col20",
                });
        }

        // **********

        [Benchmark]
        public int AddByValues()
        {
            var values = new string[] { "host1", "location1", "tenantid1" };

            this.counter1.Add(values);

            return 10;
        }

        [Benchmark]
        public int AddByValueTuple()
        {
            var values = new (string, string)[] {
                ( "host", "host1" ),
                ( "location", "location1" ),
                ( "tenantid", "tenantid1" ),
            };

            this.counter1.Add(values);

            return 10;
        }

        [Benchmark]
        public int Add2ByValueTuple()
        {
            var values = new (string, string)[] {
                ( "host", "host1" ),
                ( "location", "location1" ),
                ( "tenantid", "tenantid1" ),
            };

            this.counter1.Add2(values);

            return 10;
        }

        [Benchmark]
        public int AddByDictionary()
        {
            var labels = new Dictionary<string,string>()
            {
                { "host", "host1" },
                { "location", "location1" },
                { "tenantid", "tenantid1" },
            };

            this.counter1.Add(labels);

            return 10;
        }

        // **********

        [Benchmark]
        public int AddByLargeValues()
        {
            var values = new string[] {
                "val01", "val02", "val03", "val04", "val05", "val06", "val07", "val08", "val09", "val10",
                "val11", "val12", "val13", "val14", "val15", "val16", "val17", "val18", "val19", "val20",
            };

            this.counter2.Add(values);

            return 10;
        }

        [Benchmark]
        public int AddByLargeValueTuple()
        {
            var values = new (string, string)[] {
                ( "col01", "val01" ),
                ( "col02", "val02" ),
                ( "col03", "val03" ),
                ( "col04", "val04" ),
                ( "col05", "val05" ),
                ( "col06", "val06" ),
                ( "col07", "val07" ),
                ( "col08", "val08" ),
                ( "col09", "val09" ),
                ( "col10", "val00" ),
                ( "col11", "val11" ),
                ( "col12", "val12" ),
                ( "col13", "val13" ),
                ( "col14", "val14" ),
                ( "col15", "val15" ),
                ( "col16", "val16" ),
                ( "col17", "val17" ),
                ( "col18", "val18" ),
                ( "col19", "val19" ),
                ( "col20", "val20" ),
            };

            this.counter2.Add(values);

            return 10;
        }

        [Benchmark]
        public int Add2ByLargeValueTuple()
        {
            var values = new (string, string)[] {
                ( "col01", "val01" ),
                ( "col02", "val02" ),
                ( "col03", "val03" ),
                ( "col04", "val04" ),
                ( "col05", "val05" ),
                ( "col06", "val06" ),
                ( "col07", "val07" ),
                ( "col08", "val08" ),
                ( "col09", "val09" ),
                ( "col10", "val00" ),
                ( "col11", "val11" ),
                ( "col12", "val12" ),
                ( "col13", "val13" ),
                ( "col14", "val14" ),
                ( "col15", "val15" ),
                ( "col16", "val16" ),
                ( "col17", "val17" ),
                ( "col18", "val18" ),
                ( "col19", "val19" ),
                ( "col20", "val20" ),
            };

            this.counter2.Add2(values);

            return 10;
        }

        [Benchmark]
        public int AddByLargeDictionary()
        {
            var labels = new Dictionary<string,string>()
            {
                { "col01", "val01" },
                { "col02", "val02" },
                { "col03", "val03" },
                { "col04", "val04" },
                { "col05", "val05" },
                { "col06", "val06" },
                { "col07", "val07" },
                { "col08", "val08" },
                { "col09", "val09" },
                { "col10", "val00" },
                { "col11", "val11" },
                { "col12", "val12" },
                { "col13", "val13" },
                { "col14", "val14" },
                { "col15", "val15" },
                { "col16", "val16" },
                { "col17", "val17" },
                { "col18", "val18" },
                { "col19", "val19" },
                { "col20", "val20" },
            };

            this.counter2.Add(labels);

            return 10;
        }
    }

    public class Counter
    {
        private string[] colnames;
        private Dictionary<string,string> dict = new();


        public Counter(string[] colnames)
        {
            this.colnames = colnames;
        }

        public void Add(string [] colvalues)
        {
            dict.Clear();

            for (int i = 0; i < colnames.Length; i++)
            {
                dict[colnames[i]] = colvalues[i];
            }
        }

        public void Add(params (string name, string value)[] labels)
        {
            dict.Clear();

            for (int i = 0; i < labels.Length; i++)
            {
                dict[labels[i].name] = labels[i].value;
            }
        }

        public void Add2(params (string name, string value)[] labels)
        {
            dict.Clear();

            foreach (var label in labels)
            {
                dict[label.name] = label.value;
            }
        }

        public void Add(IDictionary<string,string> labels)
        {
            dict.Clear();

            foreach (var label in labels)
            {
                dict[label.Key] = label.Value;
            }
        }
    }
}
