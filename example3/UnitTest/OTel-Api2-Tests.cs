using System;
using Xunit;

namespace OpenTelemetry.Metric.Api2
{
    public class ApiTest
    {
        [Fact]
        public void HappyPath()
        {
            var provider = MeterProvider.Default;

            var meter = provider.GetMeter("test");

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

            if (meter is BasicMeter basicMeter)
            {
                basicMeter.Observe();
            }


            var longgauge = meter.CreateGauge<long>("longgauge");
            longgauge.Set(20);
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
    }
}
