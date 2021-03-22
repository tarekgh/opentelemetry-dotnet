using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Metric;

namespace SimpleExamples
{
    class ObservableCounter_CachedDynamicLabels_Example
    {
        ObservableCounter _hatsSoldCounter = new ObservableCounter("HatCo.ColoredHatsSold.Cached");

        LabeledObservableCounter _yellowHatsSoldCounter;
        LabeledObservableCounter _redHatsSoldCounter;

        public ObservableCounter_CachedDynamicLabels_Example()
        {
            _yellowHatsSoldCounter = _hatsSoldCounter.WithLabels(
                () => ColoredHatStoreData.GetTotalHatsSold("Yellow"), ("Color", "Yellow"));
            _redHatsSoldCounter = _hatsSoldCounter.WithLabels(
                () => ColoredHatStoreData.GetTotalHatsSold("Red"), ("Color", "Red"));
        }
    }
}
