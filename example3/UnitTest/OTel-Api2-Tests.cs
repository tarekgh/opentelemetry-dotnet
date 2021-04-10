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
                var msg = base.ToString(instrument, value, attributes);
                Console.WriteLine($"HappyPath: {msg}");
            }

            public override void Record<T>(Instrument instrument, (T value, (string name, object value)[] attributes)[] measurements)
            {
                int c = 1;
                foreach (var m in measurements)
                {
                    var msg = base.ToString(instrument, m.value, m.attributes);
                    Console.WriteLine($"HappyPath [{c}/{measurements.Length}]: {msg}");
                    c++;
                }
            }
        }

        [Fact]
        public void HappyPath()
        {
            var provider = MeterProvider.Default;

            var meter = provider.GetMeter("mylib.test", "1.0.0");

            var basicMeter = meter as BasicMeter;

            var basicProvider = provider as BasicMeterProvider;

            basicProvider.ProviderListener = new TestListener();

            // Counters

            var counter = meter.CreateCounter("counter");
            counter.Add(10, ("location", "here"), ("id", 100));

            var intcounter = meter.CreateCounter<int>("intcounter", "desc of counter", "bytes");
            intcounter.Add(20);

            var longcounter = meter.CreateCounter<long>("longcounter");
            longcounter.Add(20);

            // CounterFunc

            var counterfunc = meter.CreateCounterFunc("counterfunc",
                (observer, arg) =>
                {
                    observer.Observe(10.1);
                    observer.Observe((double)arg);
                    observer.Observe(10.3, ("location", "inhere"), ("id", 10));
                },
                (double) 100.2);

            Func<int> funcState = () => {
                return 121;
            };

            var intcounterfunc = meter.CreateCounterFunc<int>(
                name: "intcounterfunc", 
                callback: (observer, arg) =>
                {
                    observer.Observe(20, ("location", "here"), ("id", 100));
                    if (arg is Func<int> func)
                    {
                        var val = func();
                        observer.Observe(val, ("location", "there"), ("id", 221));
                    }
                    observer.Observe(22);
                }, 
                state: funcState);

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

        [Fact]
        public void ExtensionTest()
        {
            var provider = new MeterProvider();

            var basicProvider = provider.Provider as BasicMeterProvider;
            basicProvider.ProviderListener = new TestListener();

            var meter1 = basicProvider.GetMeter(typeof(ApiTest));

            var meter2 = basicProvider.GetMeter(typeof(ApiTest));

            var counter = meter1.CreateCounter("counter");
            counter.Add(100, ("location", "local1"));

            var counterfunc = meter1.CreateCounterFunc("counterfunc", (observer, arg) => {
                observer.Observe(100, ("location", "func1"));
            });

            var counter2 = meter2.CreateCounter("counter");
            counter2.Add(200, ("location", "local2"));

            var counterfunc2 = meter2.CreateCounterFunc("counterfunc", (observer, arg) => {
                observer.Observe(200, ("location", "func2"));
            });

            basicProvider.Observe();
        }

        [Fact]
        public void DisposeTest()
        {
            var provider = new MeterProvider();

            var basicProvider = provider.Provider as BasicMeterProvider;
            basicProvider.ProviderListener = new TestListener();

            using (var meter1 = basicProvider.GetMeter(typeof(ApiTest)))
            {
                var counterfunc1 = meter1.CreateCounterFunc("counterfunc", (observer, arg) => {
                    observer.Observe(100, ("location", "func1"));
                });

                using (var meter2 = basicProvider.GetMeter(typeof(ApiTest)))
                {
                    using (var counterfunc2 = meter2.CreateCounterFunc("counterfunc", (observer, arg) => {
                        observer.Observe(200, ("location", "func2"));
                        })
                        )
                    {
                        Console.WriteLine("--- 2");
                        basicProvider.Observe();
                    }

                    Console.WriteLine("--- 1");
                    basicProvider.Observe();
                }

                Console.WriteLine("--- 1");
                basicProvider.Observe();
            }

            Console.WriteLine("--- 0");
            basicProvider.Observe();
        }
    }
}
