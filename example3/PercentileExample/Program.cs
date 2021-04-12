using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Metric;
using Microsoft.OpenTelemetry.Export;
using OpenTelemetry.Metric.Sdk;

namespace PercentileExample
{
    class Program
    {
        static void Main(string[] args)
        {
            MetricProvider provider = new MetricProvider()
                .Include("PercentileExample")
                .AddExporter(new PrometheusExporter())
                .Build();

            CancellationTokenSource cts = new CancellationTokenSource();
            Task sendMeasurementsTask = Task.Run(() => SendMeasurements(cts.Token));

            Console.WriteLine("Enter to continue");
            Console.ReadLine();
            cts.Cancel();
            sendMeasurementsTask.Wait();
        }

        static void SendMeasurements(CancellationToken ct)
        {
            HttpClient client = new HttpClient();
            Meter meter = new Meter("PercentileExample");
            Distribution d = meter.CreateDistribution("ping_bing");
            Stopwatch sw = new Stopwatch();
            while (!ct.IsCancellationRequested)
            {
                sw.Start();
                client.GetAsync("http://bing.com").Wait();
                sw.Stop();
                d.Record(sw.ElapsedMilliseconds);
                sw.Reset();
            }
        }
    }
}
