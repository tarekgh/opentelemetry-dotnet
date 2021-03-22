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
    /* BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19042
        Intel Core i7-9700K CPU 3.60GHz (Coffee Lake), 1 CPU, 8 logical and 8 physical cores
        .NET Core SDK=5.0.201
          [Host]     : .NET Core 5.0.4 (CoreCLR 5.0.421.11614, CoreFX 5.0.421.11614), X64 RyuJIT
          DefaultJob : .NET Core 5.0.4 (CoreCLR 5.0.421.11614, CoreFX 5.0.421.11614), X64 RyuJIT


        |                    Method |       Mean |    Error |   StdDev |  Gen 0 | Gen 1 | Gen 2 | Allocated |
        |-------------------------- |-----------:|---------:|---------:|-------:|------:|------:|----------:|
        |             Add5xNoLabels |   112.9 ns |  0.21 ns |  0.17 ns |      - |     - |     - |         - |
        |      Add5xSameLabelNames1 |   150.3 ns |  0.09 ns |  0.08 ns |      - |     - |     - |         - |
        | Add5xDifferentLabelNames1 |   251.6 ns |  0.27 ns |  0.23 ns |      - |     - |     - |         - |
        |      Add5xSameLabelNames2 |   206.9 ns |  0.75 ns |  0.67 ns |      - |     - |     - |         - |
        | Add5xDifferentLabelNames2 |   529.2 ns |  4.88 ns |  4.57 ns |      - |     - |     - |         - |
        |      Add5xSameLabelNames3 |   743.3 ns |  2.78 ns |  2.60 ns | 0.0954 |     - |     - |     600 B |
        | Add5xDifferentLabelNames3 | 2,074.9 ns | 17.12 ns | 16.01 ns | 0.1869 |     - |     - |    1176 B |
        |       Add5xMultiRankSmall |   396.7 ns |  3.42 ns |  3.03 ns |      - |     - |     - |         - |
        |       Add5xMultiRankLarge | 1,724.5 ns | 14.30 ns | 13.38 ns | 0.1373 |     - |     - |     864 B |
    */

    [MemoryDiagnoser]
    public class CounterBench
    {
        static Meter m = new Meter("GroceryStoreExample");
        static Counter noLabels = new Counter("NoLabels", m);
        static Counter sameNames3 = new Counter("SameNames3", m);
        static Counter labels3 = new Counter("Labels3", m);
        static Counter sameNames2 = new Counter("SameNames2", m);
        static Counter labels2 = new Counter("Labels2", m);
        static Counter sameNames1 = new Counter("SameNames1", m);
        static Counter labels1 = new Counter("Labels1", m);
        static Counter multiRankSmall = new Counter("MultiRankSmall", m);
        static Counter multiRankLarge = new Counter("MultiRankLarge", m);
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
