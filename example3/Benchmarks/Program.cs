using System;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Engines;

namespace MyBenchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            foreach (var arg in args)
            {
                switch (arg.ToLower())
                {
                    case "metricvalue":
                        var metricValueSummary = BenchmarkRunner.Run<MetricValueBench>();
                        break;

                    case "labelset":
                        var labelsetSummary = BenchmarkRunner.Run<LabelSetBench>();
                        break;

                    case "proto":
                        var proto = BenchmarkRunner.Run<MetricProtoBench>();
                        break;

                    case "passing":
                        var passing = BenchmarkRunner.Run<LabelPassingBench>();
                        break;
                }
            }
        }
    }
}
