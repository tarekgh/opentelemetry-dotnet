using System;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Configs;

namespace MyBenchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            IConfig config = null;
#if DEBUG
            config = new DebugInProcessConfig();
#endif
            foreach (var arg in args)
            {
                switch (arg.ToLower())
                {
                    case "counters":
                        BenchmarkRunner.Run<Benchmarks.CounterBench>(config);
                        break;

                    case "dictlookup":
                        BenchmarkRunner.Run<Benchmarks.DictionaryLookupBench>(config);
                        break;

                    case "metricvalue":
                        var metricValueSummary = BenchmarkRunner.Run<MetricValueBench>(config);
                        break;

                    case "labelset":
                        var labelsetSummary = BenchmarkRunner.Run<LabelSetBench>(config);
                        break;

                    case "proto":
                        var proto = BenchmarkRunner.Run<MetricProtoBench>(config);
                        break;

                    case "passing":
                        var passing = BenchmarkRunner.Run<LabelPassingBench>(config);
                        break;
                }
            }
        }
    }
}
