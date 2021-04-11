using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;
using Microsoft.Diagnostics.Metric;
using Microsoft.OpenTelemetry.Export;
using OpenTelemetry.Metric.Api;
using OpenTelemetry.Metric.Sdk;
using System.Threading.Tasks;
using System.Threading;

namespace UnitTest
{
    public class UnitTest1
    {
        [Fact]
        public void CheckSizesOfMetricVsMetric2()
        {
            var items = new List<ExportItem>();

            for (int n = 0; n < 5; n++)
            {
                var item = new ExportItem();
                item.dt = DateTimeOffset.Parse("2020-01-01T10:12:13Z");
                item.MeterName = "Test";
                item.MeterVersion = "0.0.1";
                item.InstrumentName = $"MyTest.request_{n}";
                item.Labels = new MetricLabelSet(("Host", "Test"), ("Mode", "Test"));
                item.MeasurementAggregation = new SumAggregation();
                item.AggData = new (string,string)[] {
                    ("sum","100.5"),
                    ("count","100"),
                    ("min","10.2"),
                    ("max","100")
                };
                items.Add(item);
            }

            var client = new ProtoBufClient();

            var bytes1 = client.Send(items.ToArray(), true);
            Assert.Equal(1818, bytes1.Length);
            //Console.WriteLine(BitConverter.ToString(bytes1));
            client.Receive(bytes1);

            var bytes2 = client.Send(items.ToArray(), false);
            Assert.Equal(1818, bytes2.Length);
            //Console.WriteLine(BitConverter.ToString(bytes2));
            client.Receive(bytes2);
        }

        [Fact]
        public void OTelBasic()
        {
            var provider = new MetricProvider()
                .AddExporter(new ConsoleExporter("Test", 1000))
                .Build();

            var meter = MeterProvider.Global.GetMeter<UnitTest1>();
            var counter = meter.CreateCounter("request");

            counter.Add(10, ("name", "nameValue"), ("value","typeValue"));
            counter.Add(100, ("name", "nameValue2"), ("value", "typeValue2"));

            provider.Stop();
        }

        [Fact]
        public void OTelBasic2()
        {
            var meter = MeterProvider.Global.GetMeter<UnitTest1>();
            var counter = meter.CreateCounter("request");

            var provider1 = new MetricProvider()
                .AddExporter(new ConsoleExporter("Test", 1000))
                .Build();

            counter.Add(50, ("name", "noop"), ("value", "noop"));

            var provider2 = new MetricProvider()
                .AddExporter(new ConsoleExporter("Test", 1000))
                .Build();
            //MeterProvider.SetMeterProvider(new MeterProvider());

            counter.Add(10, ("name", "nameValue"), ("value", "typeValue"));
            counter.Add(100, ("name", "nameValue2"), ("value", "typeValue2"));

            provider1.Stop();
            provider2.Stop();
        }

        [Fact]
        public async Task OTelDI()
        {
            var host = Host.CreateDefaultBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddScoped<Meter>((srvprov) => {
                        return MeterProvider.Global.GetMeter<UnitTest1>();
                    });
                    services.AddHostedService<MyService>();
                })
                .Build();

            var provider1 = new MetricProvider()
                .AddExporter(new ConsoleExporter("Test", 1000))
                .Build();

            var task = host.RunAsync();

            await Task.Delay(1000);

            await host.StopAsync();

            provider1.Stop();
        }

        public class MyService : IHostedService
        {
            ILogger logger;
            Meter meter;
            Task task;
            CancellationTokenSource cts = new();

            public MyService(ILogger<MyService> logger, Meter meter)
            {
                this.logger = logger;
                this.meter = meter;
            }

            public Task StartAsync(CancellationToken cancellationToken)
            {
                task = Task.Run(RunTask);
                return task;
            }

            public Task StopAsync(CancellationToken cancellationToken)
            {
                cts.Cancel();
                return task;
            }

            public async Task RunTask()
            {
                logger.LogInformation("Started...");

                var counter = meter.CreateCounter("request");

                counter.Add(10, ("Dim1", "nameValue"), ("Dim2","typeValue"));

                while (!cts.Token.IsCancellationRequested)
                {
                    logger.LogInformation("Waiting...");
                    await Task.Delay(400);
                }

                counter.Add(100, ("Dim1","DimVal1"), ("Dim2","DimVal2"));

                logger.LogInformation("Stopped...");
            }
        }

        [Fact]
        public void OTelSimpleDI()
        {
            var services = new ServiceCollection();
            services.AddScoped<Meter>((srvprov) => {
                return MeterProvider.Global.GetMeter<UnitTest1>();
            });
            var serviceProvider = services.BuildServiceProvider();

            Task.Run(async () => {
                // Need to get IServiceProvider
                await Task.Delay(200);

                using (var scope = serviceProvider.CreateScope())
                {
                    var meter = scope.ServiceProvider.GetService<Meter>();
                    var counter = meter.CreateCounter("request");

                    for (int n = 0; n < 5; n++)
                    {
                        counter.Add(10, ("dim1","dimval1"));
                        await Task.Delay(200);
                    }
                }
            });

            var metricProvider = new MetricProvider()
                .AddExporter(new ConsoleExporter("Test", 1000))
                .Build();

            Task.Delay(2000).Wait();

            metricProvider.Stop();
        }
    }
}
