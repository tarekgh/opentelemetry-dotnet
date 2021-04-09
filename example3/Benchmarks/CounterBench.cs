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


        |                    Method |        Mean |     Error |   StdDev |  Gen 0 | Gen 1 | Gen 2 | Allocated |
        |-------------------------- |------------:|----------:|---------:|-------:|------:|------:|----------:|
        |             Add5xNoLabels |    80.61 ns |  0.339 ns | 0.301 ns |      - |     - |     - |         - |
        |      Add5xSameLabelNames1 |   174.02 ns |  0.688 ns | 0.610 ns |      - |     - |     - |         - |
        | Add5xDifferentLabelNames1 | 1,226.38 ns |  8.048 ns | 7.528 ns | 0.2155 |     - |     - |    1360 B |
        |      Add5xSameLabelNames2 |   227.96 ns |  1.329 ns | 1.243 ns |      - |     - |     - |         - |
        | Add5xDifferentLabelNames2 | 1,666.12 ns |  5.898 ns | 4.925 ns | 0.2346 |     - |     - |    1480 B |
        |      Add5xSameLabelNames3 |   283.36 ns |  0.570 ns | 0.533 ns |      - |     - |     - |         - |
        |      Add5xSameLabelNames4 |   493.46 ns |  1.989 ns | 1.661 ns | 0.1144 |     - |     - |     720 B |
        | Add5xDifferentLabelNames3 | 2,235.50 ns |  5.835 ns | 4.873 ns | 0.2518 |     - |     - |    1600 B |
        |       Add5xMultiRankSmall |   914.56 ns |  7.647 ns | 7.153 ns | 0.1297 |     - |     - |     816 B |
        |       Add5xMultiRankLarge | 1,611.73 ns | 10.248 ns | 9.586 ns | 0.2346 |     - |     - |    1472 B |
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

        
        [Benchmark]
        public void Add5xNoLabels()
        {
            noLabels.Add(1);
            noLabels.Add(1);
            noLabels.Add(1);
            noLabels.Add(1);
            noLabels.Add(1);
        }

        [Benchmark]
        public void Add5xSameLabelNames1()
        {
            sameNames1.Add(1, ("Color", "Red"));
            sameNames1.Add(1, ("Color", "Blue"));
            sameNames1.Add(1, ("Color", "Green"));
            sameNames1.Add(1, ("Color", "Orange"));
            sameNames1.Add(1, ("Color", "Yellow"));
        }

        
        [Benchmark]
        public void Add5xDifferentLabelNames1()
        {
            labels1.Add(1, ("ColorR", "Red"));
            labels1.Add(1, ("ColorB", "Blue"));
            labels1.Add(1, ("ColorG", "Green"));
            labels1.Add(1, ("ColorO", "Orange"));
            labels1.Add(1, ("ColorY", "Yellow"));
        }

        [Benchmark]
        public void Add5xSameLabelNames2()
        {
            sameNames2.Add(1, ("Color", "Red"), ("Size", "1"));
            sameNames2.Add(1, ("Color", "Blue"), ("Size", "1"));
            sameNames2.Add(1, ("Color", "Green"), ("Size", "1"));
            sameNames2.Add(1, ("Color", "Orange"), ("Size", "1"));
            sameNames2.Add(1, ("Color", "Yellow"), ("Size", "1"));
        }

        
        [Benchmark]
        public void Add5xDifferentLabelNames2()
        {
            labels2.Add(1, ("ColorR", "Red"), ("Size", "1"));
            labels2.Add(1, ("ColorB", "Blue"), ("Size", "1"));
            labels2.Add(1, ("ColorG", "Green"), ("Size", "1"));
            labels2.Add(1, ("ColorO", "Orange"), ("Size", "1"));
            labels2.Add(1, ("ColorY", "Yellow"), ("Size", "1"));
        }

        [Benchmark]
        public void Add5xSameLabelNames3()
        {
            sameNames3.Add(1, ("Color", "Red"), ("Size", "1"), ("Zoo","True"));
            sameNames3.Add(1, ("Color", "Blue"), ("Size", "1"), ("Zoo", "True"));
            sameNames3.Add(1, ("Color", "Green"), ("Size", "1"), ("Zoo", "True"));
            sameNames3.Add(1, ("Color", "Orange"), ("Size", "1"), ("Zoo", "True"));
            sameNames3.Add(1, ("Color", "Yellow"), ("Size", "1"), ("Zoo", "True"));
        }

        [Benchmark]
        public void Add5xSameLabelNames4()
        {
            sameNames4.Add(1, ("Color", "Red"), ("Size", "1"), ("Zoo", "True"), ("Zoo2", "True"));
            sameNames4.Add(1, ("Color", "Blue"), ("Size", "1"), ("Zoo", "True"), ("Zoo2", "True"));
            sameNames4.Add(1, ("Color", "Green"), ("Size", "1"), ("Zoo", "True"), ("Zoo2", "True"));
            sameNames4.Add(1, ("Color", "Orange"), ("Size", "1"), ("Zoo", "True"), ("Zoo2", "True"));
            sameNames4.Add(1, ("Color", "Yellow"), ("Size", "1"), ("Zoo", "True"), ("Zoo2", "True"));
        }

        
        [Benchmark]
        public void Add5xDifferentLabelNames3()
        {
            labels3.Add(1, ("ColorR", "Red"), ("Size", "1"), ("Zoo", "True"));
            labels3.Add(1, ("ColorB", "Blue"), ("Size", "1"), ("Zoo", "True"));
            labels3.Add(1, ("ColorG", "Green"), ("Size", "1"), ("Zoo", "True"));
            labels3.Add(1, ("ColorO", "Orange"), ("Size", "1"), ("Zoo", "True"));
            labels3.Add(1, ("ColorY", "Yellow"), ("Size", "1"), ("Zoo", "True"));
        }

        [Benchmark]
        public void Add5xMultiRankSmall()
        {
            multiRankSmall.Add(1);
            multiRankSmall.Add(1, ("Color", "Blue"));
            multiRankSmall.Add(1, ("Color", "Green"), ("Size", "1"));
            multiRankSmall.Add(1, ("Color", "Orange"), ("Size", "1"));
            multiRankSmall.Add(1, ("Color", "Yellow"), ("Size", "1"));
        }

        [Benchmark]
        public void Add5xMultiRankLarge()
        {
            multiRankLarge.Add(1, ("Extra", "1"));
            multiRankLarge.Add(1, ("Color", "Blue"), ("Extra", "1"), ("Extra2", "2"));
            multiRankLarge.Add(1, ("Color", "Green"), ("Size", "1"), ("Extra", "1"), ("Extra2", "2"));
            multiRankLarge.Add(1, ("Color", "Orange"), ("Size", "1"), ("Extra", "1"), ("Extra2", "2"));
            multiRankLarge.Add(1, ("Color", "Yellow"), ("Size", "1"), ("Extra", "1"), ("Extra2", "2"));
        }
    }
}
