using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Linq;
using Xunit;

namespace OpenTelemetry.Metric.Api2
{
    public class MeterProvider
    {
        private static MeterProvider defaultProvider = new ();
        private ConcurrentDictionary<(string name, string version), Meter> meters = new ();

        public MeterProvider()
        {
        }

        public static MeterProvider Default
        {
            get
            {
                return MeterProvider.defaultProvider;
            }

            set
            {
                MeterProvider.defaultProvider = value;
            }
        }

        public static MeterProvider SetDefaultProvider(MeterProvider meterProvider)
        {
            return Interlocked.Exchange(ref MeterProvider.defaultProvider, meterProvider);
        }

        public virtual Meter GetMeter(string name, string version = null)
        {
            Meter meter = null;
            Meter newMeter = null;

            while (meter is null)
            {
                if (!meters.TryGetValue((name, version), out meter))
                {
                    if (newMeter is null)
                    {
                        newMeter = new Meter(this, name, version);
                    }

                    if (meters.TryAdd((name, version), newMeter))
                    {
                        meter = newMeter;
                    }
                }
            }

            return meter;
        }
    }

    public class Meter
    {
        public ConcurrentBag<Instrument> Instruments { get; } = new();
        public MeterProvider Provider { get; init; }
        public string Name { get; init; }
        public string Version { get; init; }

        internal Meter(MeterProvider provider, string name, string version)
        {
            this.Provider = provider;
            this.Name = name;
            this.Version = version;
        }

        public Counter CreateCounter(string name, string description = null, string unit = null)
        {
            var instrument = new Counter(this, name, description, unit);
            Instruments.Add(instrument);
            return instrument;
        }

        public Counter<T> CreateCounter<T>(string name, string description = null, string unit = null)
        {
            var instrument = new Counter<T>(this, name, description, unit);
            Instruments.Add(instrument);
            return instrument;
        }

        public CounterFunc CreateCounterFunc(string name, Action<Observer, object> callback, object userarg, string description = null, string unit = null)
        {
            var instrument = new CounterFunc(this, name, callback, userarg, description, unit);
            Instruments.Add(instrument);
            return instrument;
        }

        public CounterFunc<T> CreateCounterFunc<T>(string name, Action<Observer, object> callback, object userarg, string description = null, string unit = null)
        {
            var instrument = new CounterFunc<T>(this, name, callback, userarg, description, unit);
            Instruments.Add(instrument);
            return instrument;
        }

        public void Observe()
        {
            foreach (var instrument in Instruments)
            {
                if (instrument is ObservableInstrument<int> obvint)
                {
                    obvint.Observe();
                }
                else if (instrument is ObservableInstrument<long> obvlong)
                {
                    obvlong.Observe();
                }
                else if (instrument is ObservableInstrument<double> obvdouble)
                {
                    obvdouble.Observe();
                }
            }
        }
    }

    public class Instrument
    {
        public Meter MyMeter { get; init; }
        public string Name { get; init; }
        public string Description { get; init; }
        public string Unit { get; init; }

        public Instrument(Meter meter, string name, string description, string unit)
        {
            this.MyMeter = meter;
            this.Name = name;
            this.Description = description;
            this.Unit = unit;
        }

        internal void Record<T>((T value, (string name, object value)[] attributes)[] measurements)
        {
            int c = 0;
            foreach (var m in measurements)
            {
                var attr = string.Join(",", m.attributes.Select(k => $"{k.name}={k.value}"));
                Console.WriteLine($"Record [{c+1}/{measurements.Length}]: {Name}[{attr}] = {m.value.GetType().Name}/{m.value}");
                c++;
            }
        }

        internal void Record<T>(T value, (string name, object value)[] attributes)
        {
            var attr = string.Join(",", attributes.Select(k => $"{k.name}={k.value}"));
            Console.WriteLine($"Record: {Name}[{attr}] = {value.GetType().Name}/{value}");
        }
    }

    public class ObservableInstrument<T> : Instrument
    {
        internal Action<Observer<T>, object> callback;
        internal object userarg;

        public ObservableInstrument(Meter meter, string name, Action<Observer<T>, object> callback, object userarg, string description, string unit)
            : base(meter, name, description, unit)
        {
            this.callback = callback;
            this.userarg = userarg;
        }

        internal virtual void Observe()
        {
            var obv = new Observer<T>();
            this.callback(obv, userarg);

            this.Record<T>(obv.measures.ToArray());
        }
    }
    public abstract class Observer
    {
        public abstract void Observe<T>(T value, params (string name, object value)[] attributes);
    }

    public class Observer<T> : Observer
    {
        internal List<(T value, (string name, object value)[] attributes)> measures = new();

        public override void Observe<T1>(T1 value, params (string name, object value)[] attributes)
        {
            if (value is T v)
            {
                measures.Add((v, attributes));
            }
        }
    }

    public class Counter : Instrument
    {
        internal Counter(Meter meter, string name, string description, string unit)
            : base(meter, name, description, unit)
        {
        }

        public void Add(double value, params (string name, object value)[] attributes)
        {
            this.Record<double>(value, attributes);
        }
    }

    public class Counter<T> : Counter
    {
        internal Counter(Meter meter, string name, string description, string unit)
            : base(meter, name, description, unit)
        {
        }

        public virtual void Add(T value, params (string name, object value)[] attributes)
        {
            this.Record<T>(value, attributes);
        }
    }

    public class CounterFunc : ObservableInstrument<double>
    {
        internal CounterFunc(Meter meter, string name, Action<Observer, object> callback, object userarg, string description, string unit)
            : base(meter, name, callback, userarg, description, unit)
        {
        }
    }

    public class CounterFunc<T> : ObservableInstrument<T>
    {
        internal CounterFunc(Meter meter, string name, Action<Observer, object> callback, object userarg, string description, string unit)
            : base(meter, name, callback, userarg, description, unit)
        {
        }
    }

    public class OTelApiTest
    {
        [Fact]
        public void Test1()
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

            Func<int> f = () => {
                return 121;
            };

            var intcounterfunc = meter.CreateCounterFunc<int>("intcounterfunc", (obv, arg) => {
                obv.Observe(20, ("location", "here"), ("id", 100));
                if (arg is Func<int> func)
                {
                    var val = func();
                    obv.Observe(val, ("location", "tere"), ("id", 221));
                }
                obv.Observe(22);
            }, (object) f);

            meter.Observe();
        }
    }
}
