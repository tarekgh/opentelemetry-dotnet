using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Engines;

using Microsoft.Diagnostics.Metric;
using Microsoft.OpenTelemetry.Export;
using OpenTelemetry.Metric.Api;
using OpenTelemetry.Metric.Sdk;

/*
BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19042
Intel Core i7-1065G7 CPU 1.30GHz, 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=5.0.201
  [Host]     : .NET Core 5.0.4 (CoreCLR 5.0.421.11614, CoreFX 5.0.421.11614), X64 RyuJIT
  Job-OFHADH : .NET Core 5.0.4 (CoreCLR 5.0.421.11614, CoreFX 5.0.421.11614), X64 RyuJIT

IterationCount=15  LaunchCount=3  WarmupCount=3

|         Method |     Mean |     Error |    StdDev |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|--------------- |---------:|----------:|----------:|-------:|------:|------:|----------:|
|     SendMetric | 5.578 us | 0.0648 us | 0.1233 us | 1.0681 |     - |     - |   4.39 KB |
|    SendMetric2 | 5.478 us | 0.0714 us | 0.1324 us | 1.0986 |     - |     - |   4.52 KB |
|  ReceiveMetric | 5.909 us | 0.0758 us | 0.1424 us | 1.7090 |     - |     - |   7.01 KB |
| ReceiveMetric2 | 5.983 us | 0.1458 us | 0.2774 us | 1.7395 |     - |     - |   7.13 KB |
*/

namespace MyBenchmark
{
    [SimpleJob(launchCount: 3, warmupCount: 3, targetCount: 15)]
    [MemoryDiagnoser]
    public class MetricProtoBench
    {
        private ProtoBufClient client;
        ExportItem[] items;
        byte[] bytes1;
        byte[] bytes2;

        [GlobalSetup]
        public void Setup()
        {
            var items = new List<ExportItem>();

            for (int i = 0; i < 1; i++)
            {
                var item = new ExportItem();
                item.dt = DateTimeOffset.Parse("2020-01-01T10:12:13Z");
                item.MeterName = "Test";
                item.MeterVersion = "0.0.1";
                item.InstrumentName = $"MyTest.request_{i}";
                item.Labels = new MetricLabelSet(("Host", "Test"), ("Mode", "Test"));
                item.AggregationConfig = new SumAggregation();
                item.AggData = new (string,string)[] {
                    ("sum","100.5"),
                    ("count","100"),
                    ("min","10.2"),
                    ("max","100")
                };
                items.Add(item);
            }

            this.items = items.ToArray();

            client = new ProtoBufClient();

            bytes1 = client.Send(items.ToArray(), true);
            bytes2 = client.Send(items.ToArray(), false);
        }

        [Benchmark]
        public byte[] SendMetric()
        {
            return client.Send(items, true);
        }

        [Benchmark]
        public byte[] SendMetric2()
        {
            return client.Send(items, false);
        }

        [Benchmark]
        public ProtoBufClient.ParseRecord[] ReceiveMetric()
        {
            return client.ParsePayload(bytes1);
        }

        [Benchmark]
        public ProtoBufClient.ParseRecord[] ReceiveMetric2()
        {
            return client.ParsePayload(bytes2);
        }
    }
}
