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
        .NET Core SDK=5.0.202
          [Host]     : .NET Core 5.0.5 (CoreCLR 5.0.521.16609, CoreFX 5.0.521.16609), X64 RyuJIT
          DefaultJob : .NET Core 5.0.5 (CoreCLR 5.0.521.16609, CoreFX 5.0.521.16609), X64 RyuJIT


        |                  Method |       Mean |     Error |     StdDev |  Gen 0 | Gen 1 | Gen 2 | Allocated |
        |------------------------ |-----------:|----------:|-----------:|-------:|------:|------:|----------:|
        |             AddNoLabels |  20.626 ns | 0.0578 ns |  0.0540 ns |      - |     - |     - |         - |
        |              RandomNext |   8.109 ns | 0.0199 ns |  0.0177 ns |      - |     - |     - |         - |
        |    DistributionNoLabels |  36.850 ns | 0.0846 ns |  0.0750 ns |      - |     - |     - |         - |
        |      AddSameLabelNames1 |  29.983 ns | 0.0788 ns |  0.0698 ns |      - |     - |     - |         - |
        | AddDifferentLabelNames1 | 262.870 ns | 4.4975 ns |  3.7557 ns | 0.0548 |     - |     - |     344 B |
        |      AddSameLabelNames2 |  39.937 ns | 0.1153 ns |  0.1079 ns |      - |     - |     - |         - |
        | AddDifferentLabelNames2 | 363.361 ns | 6.8562 ns | 11.8266 ns | 0.0587 |     - |     - |     368 B |
        |      AddSameLabelNames3 |  50.973 ns | 0.0907 ns |  0.0804 ns |      - |     - |     - |         - |
        |      AddSameLabelNames4 | 106.251 ns | 2.0857 ns |  1.9509 ns | 0.0229 |     - |     - |     144 B |
        | AddDifferentLabelNames3 | 472.157 ns | 8.2714 ns |  6.9070 ns | 0.0625 |     - |     - |     392 B |
        |       AddMultiRankSmall | 236.080 ns | 4.7208 ns |  4.4159 ns | 0.0420 |     - |     - |     264 B |
        |       AddMultiRankLarge | 536.597 ns | 6.6272 ns |  5.8748 ns | 0.0801 |     - |     - |     504 B |
    */

    [MemoryDiagnoser]
    [EtwProfiler]
    public class CounterBench
    {
        static Meter m = new Meter("GroceryStoreExample");
        static Distribution<double> d = m.CreateDistribution<double>("Distribution");
        static Counter<double> noLabels = m.CreateCounter<double>("NoLabels");
        static Counter<double> sameNames4 = m.CreateCounter<double>("SameNames4");
        static Counter<double> sameNames3 = m.CreateCounter<double>("SameNames3");
        static Counter<double> labels3 = m.CreateCounter<double>("Labels3");
        static Counter<double> sameNames2 = m.CreateCounter<double>("SameNames2");
        static Counter<double> labels2 = m.CreateCounter<double>("Labels2");
        static Counter<double> sameNames1 = m.CreateCounter<double>("SameNames1");
        static Counter<double> labels1 = m.CreateCounter<double>("Labels1");
        static Counter<double> multiRankSmall = m.CreateCounter<double>("MultiRankSmall");
        static Counter<double> multiRankLarge = m.CreateCounter<double>("MultiRankLarge");
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
