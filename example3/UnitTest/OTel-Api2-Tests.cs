using System;
using Xunit;

namespace OpenTelemetry.Metric.Api2
{
    public class ApiTest
    {
        public class TestListener : BasicMeterProviderListener
        {
            public override void Record<T>(Instrument instrument, T value, (string name, object value)[] attributes)
            {
                var msg = base.ToString(instrument, 0, 1, value, attributes);
                Console.WriteLine($"HappyPath: {msg}");
            }

            public override void Record<T>(Instrument instrument, (T value, (string name, object value)[] attributes)[] measurements)
            {
                int c = 0;
                foreach (var m in measurements)
                {
                    var msg = base.ToString(instrument, c, measurements.Length, m.value, m.attributes);
                    Console.WriteLine($"HappyPath: {msg}");
                    c++;
                }
            }
        }

        [Fact]
        public void HappyPath()
        {
            var provider = MeterProvider.Default;

            var meter = provider.GetMeter("test");

            var basicMeter = meter as BasicMeter;

            var basicProvider = provider as BasicMeterProvider;

            basicProvider.ProviderListener = new TestListener();

            // Counters

            var counter = meter.CreateCounter("counter");
            counter.Add(10, ("location", "here"), ("id", 100));

            var intcounter = meter.CreateCounter<int>("intcounter");
            intcounter.Add(20);

            var longcounter = meter.CreateCounter<long>("longcounter");
            longcounter.Add(20);

            var counterfunc = meter.CreateCounterFunc("counterfunc", (obv, arg) => {
                obv.Observe(10.1);
                obv.Observe((double)arg);
                obv.Observe(10.3);
            }, (double) 100.2);

            Func<int> funcAsArg = () => {
                return 121;
            };

            var intcounterfunc = meter.CreateCounterFunc<int>(
                name: "intcounterfunc", 
                callback: (observer, arg) => {
                    observer.Observe(20, ("location", "here"), ("id", 100));
                    if (arg is Func<int> func)
                    {
                        var val = func();
                        observer.Observe(val, ("location", "tere"), ("id", 221));
                    }
                    observer.Observe(22);
                }, 
                state: funcAsArg);

            // Gauges

            var intgauge = meter.CreateGauge<int>("intgauge");
            intgauge.Set(400);

            var longgaugefunc = meter.CreateGaugeFunc<long>("longgaugefunc", (obv, arg) => {
                obv.Observe(410);
                obv.Observe((long)arg);
                obv.Observe(430);
            }, 420L);

            basicMeter.Observe();
        }

        [Fact]
        public void NameExistTest()
        {
            int numExceptions = 0;

            var provider = MeterProvider.Default;

            var meter = provider.GetMeter("test");

            try
            {
                var counter = meter.CreateCounter("counter");
            }
            catch (ArgumentException)
            {
                numExceptions++;
            }

            try
            {
                // Should throw exception
                var counter = meter.CreateCounter("counter");
            }
            catch (ArgumentException)
            {
                numExceptions++;
            }

            try
            {
                var counter = meter.CreateCounter("counter1");
            }
            catch (ArgumentException)
            {
                numExceptions++;
            }

            try
            {
                // Should throw exception
                var counter = meter.CreateCounter("counter");
            }
            catch (ArgumentException)
            {
                numExceptions++;
            }

            Assert.Equal(2, numExceptions);
        }

        [Fact]
        public void DistinctGetMeterTest()
        {
            var provider = MeterProvider.Default;

            var meter1 = provider.GetMeter("test");

            var meter2 = provider.GetMeter("test");

            var counter1 = meter1.CreateCounter("counter");

            var counter2 = meter2.CreateCounter("counter");
        }
    }
}
