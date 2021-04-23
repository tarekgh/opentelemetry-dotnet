using System;
using System.Diagnostics;
using Microsoft.Diagnostics.Metric;
using System.Collections.Generic;

namespace NewExample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Hello Metric!");

            //
            // Unrelated Metrics Stuff
            // 

            Random random = new Random();

            static (string, string)[] CreateLabels(int dimension)
            {
                (string, string)[] labels = new (string, string)[dimension];
                for (int i = 0; i < dimension; i++)
                {
                    labels[i] = ("Lab" + i, "Val" + i);
                }
                return labels;
            }

            //
            // Create a Meter
            //

            Meter meter = new Meter("System.Diagnostics.Metrics", "v1.0");

            //
            // Create a simple Counter
            //

            Counter<long> longCounter = meter.CreateInt64Counter(name: "intCounter1", description: "Long Counter 1", unit: "Int64");

            //
            // Create a InstrumentListener
            //

            MeterInstrumentListener<long> longListener = new MeterInstrumentListener<long>()
            {
                MeasurementRecorded = (instrument, value, labels, cookie) =>
                {
                    Console.Write($"{instrument.Name} recorded the vale {value} with the labels: [");
                    for (int i = 0; i < labels.Length; i++)
                    {
                        string s = i == labels.Length - 1 ? "" : ", ";
                        Console.Write($"({labels[i].Item1}, {labels[i].Item2}){s}");
                    }
                    Console.WriteLine("]");
                }
            };

            //
            // Create a MeterListener
            //

            CounterFunc<double> encounteredInstrument = null;

            MeterListener meterListener = new MeterListener()
            {
                ShouldListenTo = (m) => m.Name == "System.Diagnostics.Metrics",
                InstrumentEncountered = (instrument) => // *InstrumentCreated*, InstrumentPublished
                {
                    if (instrument.Name == "intCounter1")
                    {
                        Debug.Assert(instrument is MeterInstrument<long>);
                        ((MeterInstrument<long>)instrument).AddListener(longListener, null); // Enable Counter Listener
                    }
                    else if (instrument.Name == "DoubleObseravbleCounter")
                    {
                        Debug.Assert(instrument is CounterFunc<double>);
                        encounteredInstrument = (CounterFunc<double>)instrument;
                    }
                }
            };

            Debug.Assert(!longCounter.IsObservable);
            Debug.Assert(!longCounter.Enabled);

            Meter.AddListener(meterListener); // Enable Meter Listener

            Debug.Assert(longCounter.Enabled);

            //
            // Now publish counter values
            //

            longCounter.Add(1);
            longCounter.Add(2, ("L1", "V1"));
            longCounter.Add(3, ("L1", "V1"), ("L2", "V2"));
            longCounter.Add(4, ("L1", "V1"), ("L2", "V2"), ("L3", "V3"));

            //
            // Create Observable Counter
            //

            CounterFunc<double> doubleObservableCounter = meter.CreateDoubleCounterFunc(
                                                                    "DoubleObseravbleCounter",
                                                                    () => new MeasurementObservaion<double>[]
                                                                          {
                                                                              new MeasurementObservaion<double>(CreateLabels(1), random.NextDouble()),
                                                                              new MeasurementObservaion<double>(CreateLabels(2), random.NextDouble()),
                                                                              new MeasurementObservaion<double>(CreateLabels(3), random.NextDouble())
                                                                          }
                                                                    );

            Debug.Assert(encounteredInstrument is not null);
            Debug.Assert(encounteredInstrument.IsObservable);

            for (int i = 0; i < 5; i++)
            {
                IEnumerable<MeasurementObservaion<double>> obsevedValues = encounteredInstrument.Observe();
                foreach (MeasurementObservaion<double> value in obsevedValues)
                {
                    Console.Write($"{encounteredInstrument.Name} recorded the vale {value.Value} with the labels: [");
                    foreach ((string, string) label in value.Labels)
                    {
                        Console.Write($"({label.Item1}, {label.Item2})");
                    }
                    Console.WriteLine("]");
                }
            }

            Console.ReadLine();
        }
    }
}
