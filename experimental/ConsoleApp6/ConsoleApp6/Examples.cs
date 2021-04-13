using System;
using System.Collections.Generic;
using System.Diagnostics;


// Example library that creates some simple metrics with a few default labels
namespace SquidLibrary
{
    class SquidAnalyzer
    {
        static readonly MetricSource metrics = new MetricSource("SquidLibrary",
            defaultLabels: new Dictionary<string, string>()
            {
                { "SquidBetaBuild", "true" },
                { "SeafoodFeatureLevel", "4" }
            });
        static readonly Counter squidsAnalyzedCounter = metrics.CreateCounter("squids-analyzed");
        static readonly Gauge squidsQueued = metrics.CreateGauge("squids-queued");


        private Queue<string> queue = new Queue<string>();

        public void ProcessSquid(string squidCategory)
        {
            squidsAnalyzedCounter.Add(1);
            squidsQueued.Set(queue.Count);
        }
    }
}


// Example library that creates some simple metrics with a dynamic label
namespace SquidLibrary2
{
    class SquidAnalyzer
    {
        static readonly MetricSource metrics = new MetricSource("SquidLibrary");
        static readonly Counter squidsAnalyzedCounter = metrics.CreateCounter("squids-analyzed");
        static readonly Gauge squidsQueued = metrics.CreateGauge("squids-queued");


        private Queue<string> queue = new Queue<string>();

        public void ProcessSquid(string squidCategory)
        {
            squidsAnalyzedCounter.Add(1, "category", squidCategory);
            squidsQueued.Set(queue.Count, "category", squidCategory);
        }
    }
}

// Example library that creates some simple metrics with many dynamic labels
namespace SquidLibrary3
{
    class SquidAnalyzer
    {
        static readonly MetricSource metrics = new MetricSource("SquidLibrary");
        static readonly Counter squidsAnalyzedCounter = metrics.CreateCounter("squids-analyzed");
        static readonly Gauge squidsQueued = metrics.CreateGauge("squids-queued");


        private Queue<string> queue = new Queue<string>();

        public void ProcessSquid(string squidCategory)
        {
            // pretend these values were computed dynamically
            string labelValue2 = "shrimp";
            string labelValue3 = "clam";
            string labelValue4 = "soup";
            squidsAnalyzedCounter.Add(1,
                "category", squidCategory,
                "labelName2", labelValue2,
                "labelName3", labelValue3,
                "labelName4", labelValue4);
            squidsQueued.Set(queue.Count,
                "category", squidCategory,
                "labelName2", labelValue2,
                "labelName3", labelValue3,
                "labelName4", labelValue4);
        }
    }
}

// Example library that creates some simple metrics with a dynamic LabelSet
namespace SquidLibrary4
{
    class SquidAnalyzer
    {
        static readonly MetricSource metrics = new MetricSource("SquidLibrary");
        static readonly Counter squidsAnalyzedCounter = metrics.CreateCounter("squids-analyzed");
        static readonly Gauge squidsQueued = metrics.CreateGauge("squids-queued");


        private Queue<string> queue = new Queue<string>();

        public void ProcessSquid(string squidCategory)
        {
            // pretend these values were computed dynamically
            LabelSet labels = new LabelSet(new Dictionary<string, string>()
            {
                { "category", squidCategory},
                { "labelName2", "shrimp"},
                { "labelName3", "clam"},
                { "labelName4", "soup"},
            });

            squidsAnalyzedCounter.Add(1, labels);
            squidsQueued.Set(queue.Count, labels);
        }
    }
}
