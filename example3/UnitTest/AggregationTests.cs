using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Metric;
using OpenTelemetry.Metric.Api;
using OpenTelemetry.Metric.Sdk;
using Xunit;

namespace UnitTest
{
    public class AggregationTests
    {
        [Fact]
        public void CounterNoLabels()
        {
            var provider = new MetricProvider()
                .Include("CounterNoLabels")
                .Build();
            Meter m = new Meter("CounterNoLabels");
            Counter c = new Counter("C", m);

            c.Add(3);
            AssertCounterSum(provider, c, 3);
            c.Add(4);
            AssertCounterSum(provider, c, 7);
            c.Add(4);
            AssertCounterSum(provider, c, 11);
        }

        [Fact]
        public void Counter1Label()
        {
            var provider = new MetricProvider()
                .Include("Counter1Label")
                .Build();
            Meter m = new Meter("Counter1Label");
            Counter c = new Counter("C", m);

            c.Add(3, ("Color", "Red"));
            AssertCounterSum(provider, c, 3, ("Color", "Red"));
            c.Add(4, ("Color", "Red"));
            AssertCounterSum(provider, c, 7, ("Color", "Red"));
            c.Add(4, ("Color", "Red"));
            AssertCounterSum(provider, c, 11, ("Color", "Red"));
        }

        [Fact]
        public void Counter1LabelMultiValue()
        {
            var provider = new MetricProvider()
                .Include("Counter1LabelMultiValue")
                .Build();
            Meter m = new Meter("Counter1LabelMultiValue");
            Counter c = new Counter("C", m);

            c.Add(3, ("Color", "Red"));
            AssertCounterSum(provider, c, 3, ("Color", "Red"));
            c.Add(4, ("Color", "Blue"));
            AssertCounterSum(provider, c, 3, ("Color", "Red"));
            AssertCounterSum(provider, c, 4, ("Color", "Blue"));
            c.Add(5, ("Color", "Green"));
            AssertCounterSum(provider, c, 3, ("Color", "Red"));
            AssertCounterSum(provider, c, 4, ("Color", "Blue"));
            AssertCounterSum(provider, c, 5, ("Color", "Green"));

            c.Add(2, ("Color", "Red"));
            c.Add(5, ("Color", "Blue"));
            c.Add(7, ("Color", "Green"));
            AssertCounterSum(provider, c, 5, ("Color", "Red"));
            AssertCounterSum(provider, c, 9, ("Color", "Blue"));
            AssertCounterSum(provider, c, 12, ("Color", "Green"));
        }

        [Fact]
        public void Counter1LabelMultiName()
        {
            var provider = new MetricProvider()
                .Include("Counter1LabelMultiName")
                .Build();
            Meter m = new Meter("Counter1LabelMultiName");
            Counter c = new Counter("C", m);

            c.Add(3, ("Color", "Red"));
            AssertCounterSum(provider, c, 3, ("Color", "Red"));
            c.Add(4, ("Color", "Blue"));
            AssertCounterSum(provider, c, 3, ("Color", "Red"));
            AssertCounterSum(provider, c, 4, ("Color", "Blue"));
            c.Add(5, ("Color2", "Green"));
            AssertCounterSum(provider, c, 3, ("Color", "Red"));
            AssertCounterSum(provider, c, 4, ("Color", "Blue"));
            AssertCounterSum(provider, c, 5, ("Color2", "Green"));

            c.Add(2, ("Color", "Red"));
            c.Add(5, ("Color", "Blue"));
            c.Add(7, ("Color2", "Green"));
            AssertCounterSum(provider, c, 5, ("Color", "Red"));
            AssertCounterSum(provider, c, 9, ("Color", "Blue"));
            AssertCounterSum(provider, c, 12, ("Color2", "Green"));
        }

        [Fact]
        public void Counter2Label()
        {
            var provider = new MetricProvider()
                .Include("Counter2Label")
                .Build();
            Meter m = new Meter("Counter2Label");
            Counter c = new Counter("C", m);

            c.Add(3, ("Color", "Red"), ("Size", "1"));
            AssertCounterSum(provider, c, 3, ("Color", "Red"), ("Size", "1"));
            c.Add(4, ("Color", "Red"), ("Size", "1"));
            AssertCounterSum(provider, c, 7, ("Color", "Red"), ("Size", "1"));
            c.Add(4, ("Color", "Red"), ("Size", "1"));
            AssertCounterSum(provider, c, 11, ("Color", "Red"), ("Size", "1"));
        }

        [Fact]
        public void Counter2LabelMultiValue()
        {
            var provider = new MetricProvider()
                .Include("Counter2LabelMultiValue")
                .Build();
            Meter m = new Meter("Counter2LabelMultiValue");
            Counter c = new Counter("C", m);

            c.Add(3, ("Color", "Red"), ("Size", "1"));
            AssertCounterSum(provider, c, 3, ("Color", "Red"), ("Size", "1"));
            c.Add(4, ("Color", "Red"), ("Size", "2"));
            AssertCounterSum(provider, c, 3, ("Color", "Red"), ("Size", "1"));
            AssertCounterSum(provider, c, 4, ("Color", "Red"), ("Size", "2"));
            c.Add(5, ("Color", "Red"), ("Size", "3"));
            AssertCounterSum(provider, c, 3, ("Color", "Red"), ("Size", "1"));
            AssertCounterSum(provider, c, 4, ("Color", "Red"), ("Size", "2"));
            AssertCounterSum(provider, c, 5, ("Color", "Red"), ("Size", "3"));
            c.Add(9, ("Color", "Blue"), ("Size", "3"));
            AssertCounterSum(provider, c, 3, ("Color", "Red"), ("Size", "1"));
            AssertCounterSum(provider, c, 4, ("Color", "Red"), ("Size", "2"));
            AssertCounterSum(provider, c, 5, ("Color", "Red"), ("Size", "3"));
            AssertCounterSum(provider, c, 9, ("Color", "Blue"), ("Size", "3"));

            c.Add(1, ("Color", "Red"), ("Size", "1"));
            c.Add(2, ("Color", "Red"), ("Size", "2"));
            c.Add(3, ("Color", "Red"), ("Size", "3"));
            c.Add(4, ("Color", "Blue"), ("Size", "3"));
            AssertCounterSum(provider, c, 4, ("Color", "Red"), ("Size", "1"));
            AssertCounterSum(provider, c, 6, ("Color", "Red"), ("Size", "2"));
            AssertCounterSum(provider, c, 8, ("Color", "Red"), ("Size", "3"));
            AssertCounterSum(provider, c, 13, ("Color", "Blue"), ("Size", "3"));
        }

        [Fact]
        public void Counter2LabelMultiName()
        {
            var provider = new MetricProvider()
                .Include("Counter2LabelMultiName")
                .Build();
            Meter m = new Meter("Counter2LabelMultiName");
            Counter c = new Counter("C", m);

            c.Add(3, ("Color", "Red"), ("Size", "1"));
            AssertCounterSum(provider, c, 3, ("Color", "Red"), ("Size", "1"));
            c.Add(4, ("Color", "Red"), ("Size2", "1"));
            AssertCounterSum(provider, c, 3, ("Color", "Red"), ("Size", "1"));
            AssertCounterSum(provider, c, 4, ("Color", "Red"), ("Size2", "1"));
            c.Add(5, ("Color", "Red"), ("Size", "3"));
            AssertCounterSum(provider, c, 3, ("Color", "Red"), ("Size", "1"));
            AssertCounterSum(provider, c, 4, ("Color", "Red"), ("Size2", "1"));
            AssertCounterSum(provider, c, 5, ("Color", "Red"), ("Size", "3"));
            c.Add(9, ("Color2", "Red"), ("Size", "3"));
            AssertCounterSum(provider, c, 3, ("Color", "Red"), ("Size", "1"));
            AssertCounterSum(provider, c, 4, ("Color", "Red"), ("Size2", "1"));
            AssertCounterSum(provider, c, 5, ("Color", "Red"), ("Size", "3"));
            AssertCounterSum(provider, c, 9, ("Color2", "Red"), ("Size", "3"));

            c.Add(1, ("Color", "Red"), ("Size", "1"));
            c.Add(2, ("Color", "Red"), ("Size2", "1"));
            c.Add(3, ("Color", "Red"), ("Size", "3"));
            c.Add(4, ("Color2", "Red"), ("Size", "3"));
            AssertCounterSum(provider, c, 4, ("Color", "Red"), ("Size", "1"));
            AssertCounterSum(provider, c, 6, ("Color", "Red"), ("Size2", "1"));
            AssertCounterSum(provider, c, 8, ("Color", "Red"), ("Size", "3"));
            AssertCounterSum(provider, c, 13, ("Color2", "Red"), ("Size", "3"));
        }

        [Fact]
        public void Counter2LabelSortOrder()
        {
            var provider = new MetricProvider()
                .Include("Counter2LabelSortOrder")
                .Build();
            Meter m = new Meter("Counter2LabelSortOrder");
            Counter c = new Counter("C", m);

            c.Add(3, ("Color", "Red"), ("Size", "1"));
            AssertCounterSum(provider, c, 3, ("Color", "Red"), ("Size", "1"));
            c.Add(4, ("Size", "1"), ("Color", "Red"));
            AssertCounterSum(provider, c, 7, ("Color", "Red"), ("Size", "1"));
        }

        [Fact]
        public void Counter3Label()
        {
            var provider = new MetricProvider()
                .Include("Counter3Label")
                .Build();
            Meter m = new Meter("Counter3Label");
            Counter c = new Counter("C", m);

            c.Add(3, ("Color", "Red"), ("Size", "1"), ("Zoo", "True"));
            AssertCounterSum(provider, c, 3, ("Color", "Red"), ("Size", "1"), ("Zoo", "True"));
            c.Add(4, ("Color", "Red"), ("Size", "1"), ("Zoo", "True"));
            AssertCounterSum(provider, c, 7, ("Color", "Red"), ("Size", "1"), ("Zoo", "True"));
            c.Add(4, ("Color", "Red"), ("Size", "1"), ("Zoo", "True"));
            AssertCounterSum(provider, c, 11, ("Color", "Red"), ("Size", "1"), ("Zoo", "True"));
        }

        [Fact]
        public void Counter3LabelMultiValue()
        {
            var provider = new MetricProvider()
                .Include("Counter3LabelMultiValue")
                .Build();
            Meter m = new Meter("Counter3LabelMultiValue");
            Counter c = new Counter("C", m);

            c.Add(3, ("Color", "Red"), ("Size", "1"), ("Zoo", "True"));
            AssertCounterSum(provider, c, 3, ("Color", "Red"), ("Size", "1"), ("Zoo", "True"));
            c.Add(4, ("Color", "Red"), ("Size", "2"), ("Zoo", "True"));
            AssertCounterSum(provider, c, 3, ("Color", "Red"), ("Size", "1"), ("Zoo", "True"));
            AssertCounterSum(provider, c, 4, ("Color", "Red"), ("Size", "2"), ("Zoo", "True"));
            c.Add(5, ("Color", "Red"), ("Size", "3"), ("Zoo", "True"));
            AssertCounterSum(provider, c, 3, ("Color", "Red"), ("Size", "1"), ("Zoo", "True"));
            AssertCounterSum(provider, c, 4, ("Color", "Red"), ("Size", "2"), ("Zoo", "True"));
            AssertCounterSum(provider, c, 5, ("Color", "Red"), ("Size", "3"), ("Zoo", "True"));
            c.Add(9, ("Color", "Blue"), ("Size", "3"), ("Zoo", "True"));
            AssertCounterSum(provider, c, 3, ("Color", "Red"), ("Size", "1"), ("Zoo", "True"));
            AssertCounterSum(provider, c, 4, ("Color", "Red"), ("Size", "2"), ("Zoo", "True"));
            AssertCounterSum(provider, c, 5, ("Color", "Red"), ("Size", "3"), ("Zoo", "True"));
            AssertCounterSum(provider, c, 9, ("Color", "Blue"), ("Size", "3"), ("Zoo", "True"));

            c.Add(1, ("Color", "Red"), ("Size", "1"), ("Zoo", "True"));
            c.Add(2, ("Color", "Red"), ("Size", "2"), ("Zoo", "True"));
            c.Add(3, ("Color", "Red"), ("Size", "3"), ("Zoo", "True"));
            c.Add(4, ("Color", "Blue"), ("Size", "3"), ("Zoo", "True"));
            AssertCounterSum(provider, c, 4, ("Color", "Red"), ("Size", "1"), ("Zoo", "True"));
            AssertCounterSum(provider, c, 6, ("Color", "Red"), ("Size", "2"), ("Zoo", "True"));
            AssertCounterSum(provider, c, 8, ("Color", "Red"), ("Size", "3"), ("Zoo", "True"));
            AssertCounterSum(provider, c, 13, ("Color", "Blue"), ("Size", "3"), ("Zoo", "True"));
        }

        [Fact]
        public void Counter3LabelMultiName()
        {
            var provider = new MetricProvider()
                .Include("Counter3LabelMultiName")
                .Build();
            Meter m = new Meter("Counter3LabelMultiName");
            Counter c = new Counter("C", m);

            c.Add(3, ("Color", "Red"), ("Size", "1"), ("Zoo", "True"));
            AssertCounterSum(provider, c, 3, ("Color", "Red"), ("Size", "1"), ("Zoo", "True"));
            c.Add(4, ("Color", "Red"), ("Size2", "1"), ("Zoo", "True"));
            AssertCounterSum(provider, c, 3, ("Color", "Red"), ("Size", "1"), ("Zoo", "True"));
            AssertCounterSum(provider, c, 4, ("Color", "Red"), ("Size2", "1"), ("Zoo", "True"));
            c.Add(5, ("Color", "Red"), ("Size", "3"), ("Zoo", "True"));
            AssertCounterSum(provider, c, 3, ("Color", "Red"), ("Size", "1"), ("Zoo", "True"));
            AssertCounterSum(provider, c, 4, ("Color", "Red"), ("Size2", "1"), ("Zoo", "True"));
            AssertCounterSum(provider, c, 5, ("Color", "Red"), ("Size", "3"), ("Zoo", "True"));
            c.Add(9, ("Color2", "Red"), ("Size", "3"), ("Zoo", "True"));
            AssertCounterSum(provider, c, 3, ("Color", "Red"), ("Size", "1"), ("Zoo", "True"));
            AssertCounterSum(provider, c, 4, ("Color", "Red"), ("Size2", "1"), ("Zoo", "True"));
            AssertCounterSum(provider, c, 5, ("Color", "Red"), ("Size", "3"), ("Zoo", "True"));
            AssertCounterSum(provider, c, 9, ("Color2", "Red"), ("Size", "3"), ("Zoo", "True"));

            c.Add(1, ("Color", "Red"), ("Size", "1"), ("Zoo", "True"));
            c.Add(2, ("Color", "Red"), ("Size2", "1"), ("Zoo", "True"));
            c.Add(3, ("Color", "Red"), ("Size", "3"), ("Zoo", "True"));
            c.Add(4, ("Color2", "Red"), ("Size", "3"), ("Zoo", "True"));
            AssertCounterSum(provider, c, 4, ("Color", "Red"), ("Size", "1"), ("Zoo", "True"));
            AssertCounterSum(provider, c, 6, ("Color", "Red"), ("Size2", "1"), ("Zoo", "True"));
            AssertCounterSum(provider, c, 8, ("Color", "Red"), ("Size", "3"), ("Zoo", "True"));
            AssertCounterSum(provider, c, 13, ("Color2", "Red"), ("Size", "3"), ("Zoo", "True"));
        }

        [Fact]
        public void Counter3LabelSortOrder()
        {
            var provider = new MetricProvider()
                .Include("Counter3LabelSortOrder")
                .Build();
            Meter m = new Meter("Counter2LabelSortOrder");
            Counter c = new Counter("C", m);

            c.Add(1, ("Color", "Red"), ("Size", "1"), ("Zoo", "True"));
            AssertCounterSum(provider, c, 1, ("Color", "Red"), ("Size", "1"), ("Zoo", "True"));
            c.Add(1, ("Size", "1"), ("Color", "Red"), ("Zoo", "True"));
            AssertCounterSum(provider, c, 2, ("Color", "Red"), ("Size", "1"), ("Zoo", "True"));
            c.Add(1, ("Size", "1"), ("Zoo", "True"), ("Color", "Red"));
            AssertCounterSum(provider, c, 3, ("Color", "Red"), ("Size", "1"), ("Zoo", "True"));
            c.Add(1, ("Color", "Red"), ("Zoo", "True"), ("Size", "1"));
            AssertCounterSum(provider, c, 4, ("Color", "Red"), ("Size", "1"), ("Zoo", "True"));
            c.Add(1, ("Zoo", "True"), ("Color", "Red"), ("Size", "1"));
            AssertCounterSum(provider, c, 5, ("Color", "Red"), ("Size", "1"), ("Zoo", "True"));
            c.Add(1, ("Zoo", "True"), ("Size", "1"), ("Color", "Red"));
            AssertCounterSum(provider, c, 6, ("Color", "Red"), ("Size", "1"), ("Zoo", "True"));
        }

        [Fact]
        public void CounterMultiRank0Start()
        {
            var provider = new MetricProvider()
                .Include("CounterMultiRank0Start")
                .Build();
            Meter m = new Meter("CounterMultiRank0Start");
            Counter c = new Counter("C", m);

            c.Add(3);
            AssertCounterSum(provider, c, 3);
            c.Add(4, ("Color", "Red"));
            AssertCounterSum(provider, c, 3);
            AssertCounterSum(provider, c, 4, ("Color", "Red"));
            c.Add(4);
            c.Add(5, ("Color", "Red"));
            AssertCounterSum(provider, c, 7);
            AssertCounterSum(provider, c, 9, ("Color", "Red"));
        }

        [Fact]
        public void CounterMultiRank1Start()
        {
            var provider = new MetricProvider()
                .Include("CounterMultiRank1Start")
                .Build();
            Meter m = new Meter("CounterMultiRank1Start");
            Counter c = new Counter("C", m);

            c.Add(3, ("Color", "Red"));
            AssertCounterSum(provider, c, 3, ("Color", "Red"));
            c.Add(4);
            AssertCounterSum(provider, c, 3, ("Color", "Red"));
            AssertCounterSum(provider, c, 4);
            c.Add(4, ("Color", "Red"));
            c.Add(5);
            AssertCounterSum(provider, c, 7, ("Color", "Red"));
            AssertCounterSum(provider, c, 9);
        }

        [Fact]
        public void CounterMultiRank2Start()
        {
            var provider = new MetricProvider()
                .Include("CounterMultiRank2Start")
                .Build();
            Meter m = new Meter("CounterMultiRank2Start");
            Counter c = new Counter("C", m);

            c.Add(3, ("Color", "Red"), ("Size", "1"));
            AssertCounterSum(provider, c, 3, ("Color", "Red"), ("Size", "1"));
            c.Add(4);
            AssertCounterSum(provider, c, 3, ("Color", "Red"), ("Size", "1"));
            AssertCounterSum(provider, c, 4);
            c.Add(4, ("Color", "Red"), ("Size", "1"));
            c.Add(5);
            AssertCounterSum(provider, c, 7, ("Color", "Red"), ("Size", "1"));
            AssertCounterSum(provider, c, 9);
        }

        [Fact]
        public void CounterMultiRank3Start()
        {
            var provider = new MetricProvider()
                .Include("CounterMultiRank3Start")
                .Build();
            Meter m = new Meter("CounterMultiRank3Start");
            Counter c = new Counter("C", m);

            c.Add(3, ("Color", "Red"), ("Size", "1"), ("Zoo", "True"));
            AssertCounterSum(provider, c, 3, ("Color", "Red"), ("Size", "1"), ("Zoo", "True"));
            c.Add(4);
            AssertCounterSum(provider, c, 3, ("Color", "Red"), ("Size", "1"), ("Zoo", "True"));
            AssertCounterSum(provider, c, 4);
            c.Add(4, ("Color", "Red"), ("Size", "1"), ("Zoo", "True"));
            c.Add(5);
            AssertCounterSum(provider, c, 7, ("Color", "Red"), ("Size", "1"), ("Zoo", "True"));
            AssertCounterSum(provider, c, 9);
        }

        [Fact]
        public void CounterMergeStaticLabels()
        {
            var provider = new MetricProvider()
                .Include("CounterMergeStaticLabels")
                .Build();
            Meter m = new Meter("CounterMergeStaticLabels");
            Counter c = new Counter("C", new Dictionary<string,string>() { { "StaticL1", "1" } }, m);

            c.Add(3, ("Color", "Red"));
            AssertCounterSum(provider, c, 3, ("Color", "Red"), ("StaticL1", "1"));
            c.Add(4, ("Color", "Red"));
            AssertCounterSum(provider, c, 7, ("Color", "Red"), ("StaticL1", "1"));
            c.Add(4, ("Color", "Red"));
            AssertCounterSum(provider, c, 11, ("Color", "Red"), ("StaticL1", "1"));
            c.Add(1);
            AssertCounterSum(provider, c, 1, ("StaticL1", "1"));
            c.Add(1, ("Color", "Blue"));
            AssertCounterSum(provider, c, 1, ("Color", "Blue"), ("StaticL1", "1"));
            c.Add(1, ("Color", "Blue"), ("Size","2"));
            AssertCounterSum(provider, c, 1, ("Color", "Blue"), ("Size", "2"), ("StaticL1", "1"));
            c.Add(1, ("Color", "Blue"), ("Size", "2"), ("Zoo", "Yeah"));
            // do we care about the sort order of dynamic vs. static? (alphabetical, dynamic before static, undefined?)
            AssertCounterSum(provider, c, 1, ("Color", "Blue"), ("Size", "2"), ("Zoo", "Yeah"), ("StaticL1", "1"));
        }


        static void AssertCounterSum(MetricProvider provider, Counter c, double expectedSum, params (string LabelName, string LabelValue)[] labels)
        {
            ExportItem[] items = provider.Collect();
            ExportItem item = items.Where(i => i.MeterName == c.Meter.Name && i.InstrumentName == c.Name && i.Labels.Labels.SequenceEqual(labels)).First();
            Assert.Contains(item.AggData, d => d.name == "sum" && d.value == expectedSum.ToString());
        }
    }

    public static class ProviderExtensions
    {
        static Func<MetricProvider,ExportItem[]> s_collectFunc;
        static ProviderExtensions()
        {
            s_collectFunc = typeof(MetricProvider).GetMethod("Collect", BindingFlags.NonPublic | BindingFlags.Instance).
                CreateDelegate<Func<MetricProvider, ExportItem[]>>();
        }

        public static ExportItem[] Collect(this MetricProvider provider)
        {
            return s_collectFunc(provider);
        }
    }
}