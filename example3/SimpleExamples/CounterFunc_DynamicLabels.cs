using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Metric;

namespace SimpleExamples
{
    class CounterFunc_DynamicLabels_Example
    {
        static Meter hatCo = new Meter("HatCo");
        static (string, string)[] yellowLabel = new (string, string)[1] { ("Color", "Yellow") };
        static (string, string)[] redLabel = new (string, string)[1] { ("Color", "Red") };

        CounterFunc<long> _hatsSoldCounter = hatCo.CreateCounterFunc<long>(
            name: "HatCo.ColoredHatsSold",
            observeValue: () => new MeasurementObservation<long>[] { new MeasurementObservation<long>(yellowLabel, ColoredHatStoreData.GetTotalHatsSold("Yellow")), new MeasurementObservation<long>(redLabel, ColoredHatStoreData.GetTotalHatsSold("Red")) }
        );
    }

    static class ColoredHatStoreData
    {
        static Dictionary<string, long> s_hatsSold = new Dictionary<string, long>();

        static ColoredHatStoreData()
        {
            s_hatsSold["Red"] = 0;
            s_hatsSold["Yellow"] = 0;
        }

        public static long GetTotalHatsSold(string color)
        {
            // Pretend this update was occurring asynchronously
            s_hatsSold[color] += new Random().Next(10_000);
            return s_hatsSold[color];
        }
    }
}
