using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Running;

using Microsoft.Diagnostics.Metric;
using Microsoft.OpenTelemetry.Export;
using OpenTelemetry.Metric.Api;
using OpenTelemetry.Metric.Sdk;

/*
BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19042
Intel Core i7-1065G7 CPU 1.30GHz, 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=5.0.201
  [Host]     : .NET Core 5.0.4 (CoreCLR 5.0.421.11614, CoreFX 5.0.421.11614), X64 RyuJIT
  Job-IAQTAE : .NET Core 5.0.4 (CoreCLR 5.0.421.11614, CoreFX 5.0.421.11614), X64 RyuJIT

IterationCount=20  LaunchCount=3  WarmupCount=3

|                         Method |        Mean |     Error |    StdDev |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------------------- |------------:|----------:|----------:|-------:|------:|------:|----------:|
|                AddByValueArray |    55.88 ns |  1.507 ns |  3.212 ns | 0.0114 |     - |     - |      48 B |
|            AddByNameValueArray |    61.71 ns |  0.591 ns |  1.322 ns | 0.0172 |     - |     - |      72 B |
|             AddByColValueArray |    65.89 ns |  0.785 ns |  1.690 ns | 0.0229 |     - |     - |      96 B |
|           AddByValueTupleArray |    61.71 ns |  0.775 ns |  1.685 ns | 0.0172 |     - |     - |      72 B |
|               AddByStructArray |    61.65 ns |  0.692 ns |  1.504 ns | 0.0172 |     - |     - |      72 B |
|          AddByStructEnumerator |   136.72 ns |  1.433 ns |  3.176 ns | 0.0401 |     - |     - |     168 B |
|                AddByDictionary |   172.74 ns |  1.662 ns |  3.684 ns | 0.0648 |     - |     - |     272 B |
|           LargeAddByValueArray |   349.03 ns |  3.215 ns |  7.191 ns | 0.0439 |     - |     - |     184 B |
|       LargeAddByNameValueArray |   374.21 ns |  3.991 ns |  8.760 ns | 0.0820 |     - |     - |     344 B |
|        LargeAddByColValueArray |   385.08 ns |  3.721 ns |  8.168 ns | 0.0877 |     - |     - |     368 B |
| LargeAddByLargeValueTupleArray |   378.16 ns |  3.692 ns |  8.182 ns | 0.0820 |     - |     - |     344 B |
|          LargeAddByStructArray |   389.94 ns |  5.803 ns | 12.859 ns | 0.0820 |     - |     - |     344 B |
|     LargeAddByStructEnumerator |   838.40 ns | 11.456 ns | 25.386 ns | 0.2708 |     - |     - |    1136 B |
|           LargeAddByDictionary | 1,130.51 ns | 10.742 ns | 23.352 ns | 0.5093 |     - |     - |    2136 B |
*/

namespace MyBenchmark
{
    [SimpleJob(launchCount: 3, warmupCount: 3, targetCount: 20)]
    [MemoryDiagnoser]
    public class LabelPassingBench
    {
        private string[] colnames1;
        private Counter counter1;

        private string[] colnames2;
        private Counter counter2;

        [GlobalSetup]
        public void Setup()
        {
            this.colnames1 = new string[] { "host", "location", "tenantid" };
            this.counter1 = new Counter(colnames1);

            this.colnames2 = new string[] {
                "col01", "col02", "col03", "col04", "col05", "col06", "col07", "col08", "col09", "col10",
                "col11", "col12", "col13", "col14", "col15", "col16", "col17", "col18", "col19", "col20",
                };

            this.counter2 = new Counter(colnames2);
        }

        // **********

        [Benchmark]
        public int AddByValueArray()
        {
            var values = new string[] { "host1", "location1", "tenantid1" };

            this.counter1.Add(values);

            return 10;
        }

        [Benchmark]
        public int AddByNameValueArray()
        {
            var namevalues = new string[] {
                "host", "host1",
                "location", "location1",
                "tenantid", "tenantid1",
                };

            this.counter1.Add2(namevalues);

            return 10;
        }

        [Benchmark]
        public int AddByColValueArray()
        {
            // This can be cached
            var names = new string[] {
                "host",
                "location",
                "tenantid"
                };

            var values = new string[] {
                "host1",
                "location1",
                "tenantid1"
                };

            this.counter1.Add(names, values);

            return 10;
        }

        [Benchmark]
        public int AddByValueTupleArray()
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
        public int AddByStructArray()
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
        public int AddByStructEnumerator()
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
            var labels = new Dictionary<string, string>()
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
        public int LargeAddByValueArray()
        {
            var values = new string[] {
                "val01",
                "val02",
                "val03",
                "val04",
                "val05",
                "val06",
                "val07",
                "val08",
                "val09",
                "val10",
                "val11",
                "val12",
                "val13",
                "val14",
                "val15",
                "val16",
                "val17",
                "val18",
                "val19",
                "val20",
            };

            this.counter2.Add(values);

            return 10;
        }

        [Benchmark]
        public int LargeAddByNameValueArray()
        {
            var namevalues = new string[] {
                "col01", "val01",
                "col02", "val02",
                "col03", "val03",
                "col04", "val04",
                "col05", "val05",
                "col06", "val06",
                "col07", "val07",
                "col08", "val08",
                "col09", "val09",
                "col10", "val00",
                "col11", "val11",
                "col12", "val12",
                "col13", "val13",
                "col14", "val14",
                "col15", "val15",
                "col16", "val16",
                "col17", "val17",
                "col18", "val18",
                "col19", "val19",
                "col20", "val20",
            };

            this.counter2.Add2(namevalues);

            return 10;
        }

        [Benchmark]
        public int LargeAddByColValueArray()
        {
            // This can be cached
            var names = new string[] {
                "col01",
                "col02",
                "col03",
                "col04",
                "col05",
                "col06",
                "col07",
                "col08",
                "col09",
                "col10",
                "col11",
                "col12",
                "col13",
                "col14",
                "col15",
                "col16",
                "col17",
                "col18",
                "col19",
                "col20",
                };

            var values = new string[] {
                "val01",
                "val02",
                "val03",
                "val04",
                "val05",
                "val06",
                "val07",
                "val08",
                "val09",
                "val10",
                "val11",
                "val12",
                "val13",
                "val14",
                "val15",
                "val16",
                "val17",
                "val18",
                "val19",
                "val20",
            };

            this.counter2.Add(names, values);

            return 10;
        }

        [Benchmark]
        public int LargeAddByLargeValueTupleArray()
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
        public int LargeAddByStructArray()
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

            this.counter2.Add(values);

            return 10;
        }

        [Benchmark]
        public int LargeAddByStructEnumerator()
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

            this.counter2.Add(values);

            return 10;
        }

        [Benchmark]
        public int LargeAddByDictionary()
        {
            var labels = new Dictionary<string, string>()
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
        private Dictionary<string, string> dict = new();


        public Counter(string[] colnames)
        {
            this.colnames = colnames;
        }

        public void Add(string[] colvalues)
        {
            dict.Clear();

            for (int i = 0; i < this.colnames.Length; i++)
            {
                dict[this.colnames[i]] = colvalues[i];
            }
        }

        public void Add2(string[] namevalues)
        {
            dict.Clear();

            for (int i = 0; i < namevalues.Length; i += 2)
            {
                dict[namevalues[i]] = namevalues[i + 1];
            }
        }

        public void Add(string[] names, string[] values)
        {
            dict.Clear();

            for (int i = 0; i < names.Length; i++)
            {
                dict[names[i]] = values[i];
            }
        }

        public void Add((string name, string value)[] labels)
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

        public void Add(IDictionary<string, string> labels)
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
