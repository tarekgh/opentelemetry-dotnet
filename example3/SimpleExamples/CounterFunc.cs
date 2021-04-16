using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Metric;

namespace SimpleExamples
{
    class CounterFunc_Example
    {
        static Meter hatCo = new Meter("HatCo");
        static CounterFunc<double> _hatsSoldCounter = hatCo.CreateCounterFunc<double>("HatCo.HatsSold", () => HatStoreData.GetTotalHatsSold());
    }

    static class HatStoreData
    {

        static long s_hatsSold = 0;

        public static long GetTotalHatsSold()
        {
            // Pretend this update was occurring asynchronously
            s_hatsSold += new Random().Next(10_000);
            return s_hatsSold;
        }
    }
}
