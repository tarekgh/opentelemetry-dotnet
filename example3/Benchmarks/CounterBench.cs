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
        |             Add5xNoLabels |   113.1 ns |  0.15 ns |  0.14 ns |      - |     - |     - |         - |
        |      Add5xSameLabelNames1 |   154.0 ns |  0.33 ns |  0.31 ns |      - |     - |     - |         - |
        | Add5xDifferentLabelNames1 |   268.4 ns |  0.47 ns |  0.40 ns |      - |     - |     - |         - |
        |      Add5xSameLabelNames2 |   206.5 ns |  0.37 ns |  0.34 ns |      - |     - |     - |         - |
        | Add5xDifferentLabelNames2 |   490.5 ns |  3.98 ns |  3.53 ns |      - |     - |     - |         - |
        |      Add5xSameLabelNames3 |   296.4 ns |  0.06 ns |  0.05 ns |      - |     - |     - |         - |
        | Add5xDifferentLabelNames3 | 1,321.7 ns |  6.71 ns |  6.28 ns | 0.0763 |     - |     - |     480 B |
        |       Add5xMultiRankSmall |   399.2 ns |  0.77 ns |  0.64 ns |      - |     - |     - |         - |
        |       Add5xMultiRankLarge | 1,721.5 ns | 17.44 ns | 16.31 ns | 0.1259 |     - |     - |     792 B |
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
