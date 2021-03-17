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
  Job-HUOAJJ : .NET Core 5.0.4 (CoreCLR 5.0.421.11614, CoreFX 5.0.421.11614), X64 RyuJIT

IterationCount=20  LaunchCount=5  WarmupCount=5

|                Method |        Mean |     Error |    StdDev |      Median |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|---------------------- |------------:|----------:|----------:|------------:|-------:|------:|------:|----------:|
|           AddByValues |    56.67 ns |  0.974 ns |  2.811 ns |    55.81 ns | 0.0114 |     - |     - |      48 B |
|       AddByValueTuple |    66.53 ns |  2.225 ns |  6.454 ns |    64.34 ns | 0.0172 |     - |     - |      72 B |
|           AddByStruct |    69.02 ns |  1.408 ns |  4.086 ns |    67.62 ns | 0.0172 |     - |     - |      72 B |
|      AddByIEnumStruct |   150.56 ns |  0.941 ns |  2.730 ns |   150.81 ns | 0.0401 |     - |     - |     168 B |
|       AddByDictionary |   208.17 ns |  2.874 ns |  8.292 ns |   210.07 ns | 0.0648 |     - |     - |     272 B |
|      AddByLargeValues |   400.05 ns |  2.708 ns |  7.898 ns |   398.16 ns | 0.0439 |     - |     - |     184 B |
|  AddByLargeValueTuple |   441.10 ns |  5.040 ns | 14.460 ns |   435.63 ns | 0.0820 |     - |     - |     344 B |
|      AddByLargeStruct |   436.75 ns |  3.916 ns | 11.108 ns |   437.51 ns | 0.0820 |     - |     - |     344 B |
| AddByLargeIEnumStruct |   941.86 ns | 12.256 ns | 34.967 ns |   926.68 ns | 0.2708 |     - |     - |    1136 B |
|  AddByLargeDictionary | 1,335.59 ns | 20.379 ns | 57.811 ns | 1,323.23 ns | 0.5093 |     - |     - |    2136 B |
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
        public int AddByStruct()
        {
            var values = new NameValue[] {
                new NameValue( "host", "host1" ),
                new NameValue( "location", "location1" ),
                new NameValue( "tenantid", "tenantid1" ),
            };

            this.counter1.Add(values);

            return 10;
        }

        [Benchmark]
        public int AddByIEnumStruct()
        {
            var values = new List<NameValue> {
                new NameValue( "host", "host1" ),
                new NameValue( "location", "location1" ),
                new NameValue( "tenantid", "tenantid1" ),
            };

            this.counter1.Add(values);

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
        public int AddByLargeStruct()
        {
            var values = new NameValue[] {
                new NameValue( "col01", "val01" ),
                new NameValue( "col02", "val02" ),
                new NameValue( "col03", "val03" ),
                new NameValue( "col04", "val04" ),
                new NameValue( "col05", "val05" ),
                new NameValue( "col06", "val06" ),
                new NameValue( "col07", "val07" ),
                new NameValue( "col08", "val08" ),
                new NameValue( "col09", "val09" ),
                new NameValue( "col10", "val00" ),
                new NameValue( "col11", "val11" ),
                new NameValue( "col12", "val12" ),
                new NameValue( "col13", "val13" ),
                new NameValue( "col14", "val14" ),
                new NameValue( "col15", "val15" ),
                new NameValue( "col16", "val16" ),
                new NameValue( "col17", "val17" ),
                new NameValue( "col18", "val18" ),
                new NameValue( "col19", "val19" ),
                new NameValue( "col20", "val20" ),
            };

            this.counter1.Add(values);

            return 10;
        }

        [Benchmark]
        public int AddByLargeIEnumStruct()
        {
            var values = new List<NameValue> {
                new NameValue( "col01", "val01" ),
                new NameValue( "col02", "val02" ),
                new NameValue( "col03", "val03" ),
                new NameValue( "col04", "val04" ),
                new NameValue( "col05", "val05" ),
                new NameValue( "col06", "val06" ),
                new NameValue( "col07", "val07" ),
                new NameValue( "col08", "val08" ),
                new NameValue( "col09", "val09" ),
                new NameValue( "col10", "val00" ),
                new NameValue( "col11", "val11" ),
                new NameValue( "col12", "val12" ),
                new NameValue( "col13", "val13" ),
                new NameValue( "col14", "val14" ),
                new NameValue( "col15", "val15" ),
                new NameValue( "col16", "val16" ),
                new NameValue( "col17", "val17" ),
                new NameValue( "col18", "val18" ),
                new NameValue( "col19", "val19" ),
                new NameValue( "col20", "val20" ),
            };

            this.counter1.Add(values);

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

            foreach (var label in labels)
            {
                dict[label.name] = label.value;
            }
        }

        public void Add(NameValue[] labels)
        {
            dict.Clear();

            foreach (var label in labels)
            {
                dict[label.Name] = label.Value;
            }
        }

        public void Add(IEnumerable<NameValue> labels)
        {
            dict.Clear();

            foreach (var label in labels)
            {
                dict[label.Name] = label.Value;
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

    public struct NameValue
    {
        public string Name;
        public string Value;

        public NameValue(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }
}
