using System;
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
    /*  BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19042
        Intel Core i7-9700K CPU 3.60GHz (Coffee Lake), 1 CPU, 8 logical and 8 physical cores
        .NET Core SDK=5.0.201
          [Host]     : .NET Core 5.0.4 (CoreCLR 5.0.421.11614, CoreFX 5.0.421.11614), X64 RyuJIT
          DefaultJob : .NET Core 5.0.4 (CoreCLR 5.0.421.11614, CoreFX 5.0.421.11614), X64 RyuJIT


        |                  Method |       Mean |      Error |     StdDev |  Gen 0 | Gen 1 | Gen 2 | Allocated |
        |------------------------ |-----------:|-----------:|-----------:|-------:|------:|------:|----------:|
        |             AddNoLabels |  19.919 ns |  0.0100 ns |  0.0083 ns |      - |     - |     - |         - |
        |              RandomNext |   7.727 ns |  0.0043 ns |  0.0038 ns |      - |     - |     - |         - |
        |    DistributionNoLabels |  36.646 ns |  0.1381 ns |  0.1291 ns |      - |     - |     - |         - |
        |      AddSameLabelNames1 |  29.579 ns |  0.0117 ns |  0.0104 ns |      - |     - |     - |         - |
        | AddDifferentLabelNames1 | 229.167 ns |  0.4925 ns |  0.4113 ns | 0.0548 |     - |     - |     344 B |
        |      AddSameLabelNames2 |  38.686 ns |  0.0373 ns |  0.0312 ns |      - |     - |     - |         - |
        | AddDifferentLabelNames2 | 335.541 ns |  0.8747 ns |  0.7754 ns | 0.0587 |     - |     - |     368 B |
        |      AddSameLabelNames3 |  53.148 ns |  0.4157 ns |  0.3889 ns |      - |     - |     - |         - |
        |      AddSameLabelNames4 | 103.566 ns |  1.0291 ns |  0.9626 ns | 0.0229 |     - |     - |     144 B |
        | AddDifferentLabelNames3 | 439.220 ns |  2.3239 ns |  2.1738 ns | 0.0625 |     - |     - |     392 B |
        |       AddMultiRankSmall | 210.102 ns |  1.9469 ns |  1.8212 ns | 0.0420 |     - |     - |     264 B |
        |       AddMultiRankLarge | 510.197 ns | 10.0853 ns | 12.3856 ns | 0.0801 |     - |     - |     504 B |
    */

    [MemoryDiagnoser]
    [EtwProfiler]
    public class CounterBench
    {
        static Meter m = new Meter("GroceryStoreExample");
        static Distribution d = m.CreateDistribution("Distribution");
        static Counter noLabels = m.CreateCounter("NoLabels");
        static Counter sameNames4 = m.CreateCounter("SameNames4");
        static Counter sameNames3 = m.CreateCounter("SameNames3");
        static Counter labels3 = m.CreateCounter("Labels3");
        static Counter sameNames2 = m.CreateCounter("SameNames2");
        static Counter labels2 = m.CreateCounter("Labels2");
        static Counter sameNames1 = m.CreateCounter("SameNames1");
        static Counter labels1 = m.CreateCounter("Labels1");
        static Counter multiRankSmall = m.CreateCounter("MultiRankSmall");
        static Counter multiRankLarge = m.CreateCounter("MultiRankLarge");
        static MetricProvider _provider = new MetricProvider()
                .Name("OrderPipeline1")
                .Include("GroceryStoreExample")
                .Build();

        static string[] s_values = { "1", "2" };
        static int s_counter;
        static Random s_random = new Random();

        [Benchmark]
        public void AddNoLabels()
        {
            noLabels.Add(1);
        }

        [Benchmark]
        public void RandomNext()
        {
            s_random.NextDouble();
        }

        [Benchmark]
        public void DistributionNoLabels()
        {
            d.Record(s_random.NextDouble());
        }

        [Benchmark]
        public void AddSameLabelNames1()
        {
            sameNames1.Add(1, ("Color", s_values[s_counter++ % 2]));
        }

        [Benchmark]
        public void AddDifferentLabelNames1()
        {
            labels1.Add(1, (s_values[s_counter++ % 2], "Red"));
        }

        [Benchmark]
        public void AddSameLabelNames2()
        {
            sameNames2.Add(1, ("Color", s_values[s_counter++ % 2]), ("Size", "1"));
        }

        [Benchmark]
        public void AddDifferentLabelNames2()
        {
            labels2.Add(1, (s_values[s_counter++ % 2], "Red"), ("Size", "1"));
        }

        [Benchmark]
        public void AddSameLabelNames3()
        {
            sameNames3.Add(1, ("Color", s_values[s_counter++ % 2]), ("Size", "1"), ("Zoo", "True"));
        }

        [Benchmark]
        public void AddSameLabelNames4()
        {
            sameNames4.Add(1, ("Color", s_values[s_counter++ % 2]), ("Size", "1"), ("Zoo", "True"), ("Zoo2", "True"));
        }

        [Benchmark]
        public void AddDifferentLabelNames3()
        {
            labels3.Add(1, (s_values[s_counter++ % 2], "Red"), ("Size", "1"), ("Zoo", "True"));
        }

        [Benchmark]
        public void AddMultiRankSmall()
        {
            if (s_counter++ % 2 == 0)
            {
                multiRankSmall.Add(1);
            }
            else
            {
                multiRankSmall.Add(1, ("Color", "Blue"));
            }

        }

        [Benchmark]
        public void AddMultiRankLarge()
        {
            if (s_counter++ % 2 == 0)
            {
                multiRankLarge.Add(1, ("Extra", "1"));
            }
            else
            {
                multiRankLarge.Add(1, ("Color", "Green"), ("Size", "1"), ("Extra", "1"), ("Extra2", "2"));
            }
        }
    }
}
