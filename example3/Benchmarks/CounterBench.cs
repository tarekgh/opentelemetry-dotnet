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
    [EtwProfiler]
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
