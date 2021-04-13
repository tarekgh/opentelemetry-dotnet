using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Running;

using OpenTelemetry.Metric.Sdk;

namespace MyBenchmark
{
    [SimpleJob(launchCount: 2, warmupCount: 2, targetCount: 10)]
    [MemoryDiagnoser]
    public class MetricValueBench
    {
        /*
            BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19042
            Intel Core i7-1065G7 CPU 1.30GHz, 1 CPU, 8 logical and 4 physical cores
            .NET Core SDK=5.0.102
            [Host]     : .NET Core 5.0.2 (CoreCLR 5.0.220.61120, CoreFX 5.0.220.61120), X64 RyuJIT
            Job-NFOGBM : .NET Core 5.0.2 (CoreCLR 5.0.220.61120, CoreFX 5.0.220.61120), X64 RyuJIT

            IterationCount=10  LaunchCount=2  WarmupCount=2

            |            Method |       Mean |     Error |    StdDev |  Gen 0 | Gen 1 | Gen 2 | Allocated |
            |------------------ |-----------:|----------:|----------:|-------:|------:|------:|----------:|
            |        Box_newInt |  5.0519 ns | 0.1516 ns | 0.1746 ns | 0.0057 |     - |     - |      24 B |
            |     Box_newDouble |  5.2198 ns | 0.0884 ns | 0.0983 ns | 0.0057 |     - |     - |      24 B |
            |         Box_toInt |  2.1703 ns | 0.1548 ns | 0.1721 ns |      - |     - |     - |         - |
            |      Box_toDouble |  2.1650 ns | 0.1207 ns | 0.1390 ns |      - |     - |     - |         - |
            |       Span_newInt | 10.7253 ns | 0.1427 ns | 0.1527 ns | 0.0076 |     - |     - |      32 B |
            |    Span_newDouble | 10.9797 ns | 0.2462 ns | 0.2835 ns | 0.0076 |     - |     - |      32 B |
            |        Span_toInt |  5.7165 ns | 0.1526 ns | 0.1632 ns |      - |     - |     - |         - |
            |     Span_toDouble |  6.2245 ns | 0.0903 ns | 0.1003 ns |      - |     - |     - |         - |
            |      Field_newInt |  5.0553 ns | 0.0520 ns | 0.0599 ns |      - |     - |     - |         - |
            |   Field_newDouble |  5.1087 ns | 0.1047 ns | 0.1205 ns |      - |     - |     - |         - |
            |       Field_toInt |  0.2171 ns | 0.1203 ns | 0.1385 ns |      - |     - |     - |         - |
            |    Field_toDouble |  0.2579 ns | 0.0273 ns | 0.0292 ns |      - |     - |     - |         - |
            |    Generic_newInt |  0.5304 ns | 0.0104 ns | 0.0120 ns |      - |     - |     - |         - |
            | Generic_newDouble |  0.9263 ns | 0.0266 ns | 0.0306 ns |      - |     - |     - |         - |
            |     Generic_toInt |  1.5802 ns | 0.1342 ns | 0.1546 ns |      - |     - |     - |         - |
            |  Generic_toDouble |  1.8121 ns | 0.0925 ns | 0.0990 ns |      - |     - |     - |         - |
        */

        MetricValueBox iVal;
        MetricValueBox dVal;

        MetricValueSpan iSpan;
        MetricValueSpan dSpan;

        MetricValueField iField;
        MetricValueField dField;

        MetricValueGeneric<int> iGeneric;
        MetricValueGeneric<double> dGeneric;

        [GlobalSetup]
        public void Setup()
        {
            iVal = new MetricValueBox(10);
            dVal = new MetricValueBox(10.5);

            iSpan = new MetricValueSpan(10);
            dSpan = new MetricValueSpan(10.5);

            iField = new MetricValueField(10);
            dField = new MetricValueField(10.5);

            iGeneric = new MetricValueGeneric<int>(10);
            dGeneric = new MetricValueGeneric<double>(10.5);
        }

        [Benchmark]
        public MetricValueBox Box_newInt()
        {
            return new MetricValueBox(10);
        }

        [Benchmark]
        public MetricValueBox Box_newDouble()
        {
            return new MetricValueBox(10.1);
        }

        [Benchmark]
        public int Box_toInt()
        {
            return iVal.ToInt32();
        }

        [Benchmark]
        public double Box_toDouble()
        {
            return dVal.ToDouble();
        }

        [Benchmark]
        public MetricValueSpan Span_newInt()
        {
            return new MetricValueSpan(10);
        }

        [Benchmark]
        public MetricValueSpan Span_newDouble()
        {
            return new MetricValueSpan(10.1);
        }

        [Benchmark]
        public int Span_toInt()
        {
            return iSpan.ToInt32();
        }

        [Benchmark]
        public double Span_toDouble()
        {
            return dSpan.ToDouble();
        }

        [Benchmark]
        public MetricValueField Field_newInt()
        {
            return new MetricValueField(10);
        }

        [Benchmark]
        public MetricValueField Field_newDouble()
        {
            return new MetricValueField(10.1);
        }

        [Benchmark]
        public int Field_toInt()
        {
            return iField.ToInt32();
        }

        [Benchmark]
        public double Field_toDouble()
        {
            return dField.ToDouble();
        }

        [Benchmark]
        public MetricValueGeneric<int> Generic_newInt()
        {
            return new MetricValueGeneric<int>(10);
        }

        [Benchmark]
        public MetricValueGeneric<double> Generic_newDouble()
        {
            return new MetricValueGeneric<double>(10.1);
        }

        [Benchmark]
        public int Generic_toInt()
        {
            return iGeneric.ToInt32();
        }

        [Benchmark]
        public double Generic_toDouble()
        {
            return dGeneric.ToDouble();
        }
    }
}
