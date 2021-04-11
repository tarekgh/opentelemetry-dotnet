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


        |                  Method |      Mean |    Error |   StdDev |  Gen 0 | Gen 1 | Gen 2 | Allocated |
        |------------------------ |----------:|---------:|---------:|-------:|------:|------:|----------:|
        |             AddNoLabels |  17.45 ns | 0.042 ns | 0.037 ns |      - |     - |     - |         - |
        |      AddSameLabelNames1 |  33.78 ns | 0.099 ns | 0.088 ns |      - |     - |     - |         - |
        | AddDifferentLabelNames1 | 232.26 ns | 1.200 ns | 1.002 ns | 0.0432 |     - |     - |     272 B |
        |      AddSameLabelNames2 |  44.44 ns | 0.186 ns | 0.165 ns |      - |     - |     - |         - |
        | AddDifferentLabelNames2 | 337.36 ns | 1.465 ns | 1.370 ns | 0.0467 |     - |     - |     296 B |
        |      AddSameLabelNames3 |  55.19 ns | 0.229 ns | 0.214 ns |      - |     - |     - |         - |
        |      AddSameLabelNames4 | 101.45 ns | 0.259 ns | 0.217 ns | 0.0229 |     - |     - |     144 B |
        | AddDifferentLabelNames3 | 435.11 ns | 2.107 ns | 1.971 ns | 0.0505 |     - |     - |     320 B |
        |       AddMultiRankSmall | 223.43 ns | 1.084 ns | 1.014 ns | 0.0412 |     - |     - |     260 B |
        |       AddMultiRankLarge | 475.21 ns | 2.040 ns | 1.808 ns | 0.0687 |     - |     - |     432 B |
    */

    [MemoryDiagnoser]
    [EtwProfiler]
    public class CounterBench
    {
        static Meter m = new Meter("GroceryStoreExample");
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

        [Benchmark]
        public void AddNoLabels()
        {
            noLabels.Add(1);
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
            sameNames3.Add(1, ("Color", s_values[s_counter++ % 2]), ("Size", "1"), ("Zoo","True"));
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
            if(s_counter++ % 2 == 0)
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
