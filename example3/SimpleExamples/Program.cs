using System;
using System.Threading;
using OpenTelemetry.Metric.Sdk;

namespace SimpleExamples
{
    class Program
    {
        static void Main(string[] args)
        {
            var pipeline = new MetricProvider()
                .AddExporter(new ConsoleExporter("export1", 2000))
                .Build();

            RunExamples();

            // make sure we can easily debug the example, shutdown
            // will take a long time before it starts timing out
            pipeline.Stop(TimeSpan.FromMinutes(30));
        }

        static void RunExamples()
        {
            new CounterFunc_Example();
            new CounterFunc_DynamicLabels_Example();

            //Allow some time for metrics to do their thing
            Thread.Sleep(TimeSpan.FromSeconds(5));
        }
    }
}
