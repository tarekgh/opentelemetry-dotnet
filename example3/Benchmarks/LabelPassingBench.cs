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
  Job-BKDMDV : .NET Core 5.0.4 (CoreCLR 5.0.421.11614, CoreFX 5.0.421.11614), X64 RyuJIT

IterationCount=20  LaunchCount=5  WarmupCount=5

|                         Method |        Mean |     Error |    StdDev |      Median |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------------------- |------------:|----------:|----------:|------------:|-------:|------:|------:|----------:|
|                AddByValueArray |    56.02 ns |  0.639 ns |  1.793 ns |    56.18 ns | 0.0114 |     - |     - |      48 B |
|            AddByNameValueArray |    59.65 ns |  0.438 ns |  1.208 ns |    59.29 ns | 0.0172 |     - |     - |      72 B |
|             AddByColValueArray |    56.44 ns |  1.040 ns |  2.999 ns |    55.27 ns | 0.0114 |     - |     - |      48 B |
|           AddByValueTupleArray |    68.47 ns |  1.994 ns |  5.722 ns |    68.38 ns | 0.0172 |     - |     - |      72 B |
|               AddByStructArray |    74.85 ns |  1.369 ns |  3.907 ns |    74.77 ns | 0.0172 |     - |     - |      72 B |
|          AddByStructEnumerator |   152.75 ns |  1.014 ns |  2.893 ns |   152.47 ns | 0.0401 |     - |     - |     168 B |
|                AddByDictionary |   196.10 ns |  1.182 ns |  3.334 ns |   195.50 ns | 0.0648 |     - |     - |     272 B |
|           LargeAddByValueArray |   372.22 ns |  4.989 ns | 13.988 ns |   374.63 ns | 0.0439 |     - |     - |     184 B |
|       LargeAddByNameValueArray |   468.78 ns | 25.514 ns | 71.543 ns |   432.05 ns | 0.0820 |     - |     - |     344 B |
|        LargeAddByColValueArray |   368.03 ns |  6.280 ns | 17.817 ns |   360.75 ns | 0.0439 |     - |     - |     184 B |
| LargeAddByLargeValueTupleArray |   414.58 ns | 20.140 ns | 59.384 ns |   385.98 ns | 0.0820 |     - |     - |     344 B |
|          LargeAddByStructArray |   428.95 ns |  9.260 ns | 26.118 ns |   421.31 ns | 0.0820 |     - |     - |     344 B |
|     LargeAddByStructEnumerator |   882.29 ns |  7.890 ns | 22.510 ns |   881.87 ns | 0.2708 |     - |     - |    1136 B |
|           LargeAddByDictionary | 1,144.69 ns |  6.693 ns | 18.546 ns | 1,140.56 ns | 0.5093 |     - |     - |    2136 B |
*/

namespace MyBenchmark
{
    [SimpleJob(launchCount: 5, warmupCount: 5, targetCount: 20)]
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
                "tenantid", "tenantid1" 
                };

            this.counter1.Add2(namevalues);

            return 10;
        }

        [Benchmark]
        public int AddByColValueArray()
        {
            var values = new string[] {
                "host1",
                "location1",
                "tenantid1"
                };

            this.counter1.Add(this.colnames1, values);

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

            this.counter2.Add(this.colnames2, values);

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

            for (int i = 0; i < this.colnames.Length; i++)
            {
                dict[this.colnames[i]] = colvalues[i];
            }
        }

        public void Add2(string [] namevalues)
        {
            dict.Clear();

            for (int i = 0; i < namevalues.Length; i += 2)
            {
                dict[namevalues[i]] = namevalues[i+1];
            }
        }

        public void Add(string [] names, string [] values)
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
