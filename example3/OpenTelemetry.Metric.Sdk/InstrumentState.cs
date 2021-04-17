using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Metric;

namespace OpenTelemetry.Metric.Sdk
{
    abstract class InstrumentState
    {
        // This can be called concurrently with Collect()
        public abstract void Update(double measurement, ReadOnlySpan<(string LabelName, string LabelValue)> labels);

        // This can be called concurrently with Update()
        public abstract void Collect(MeterInstrument instrument, Action<LabeledAggregationStatistics> aggregationVisitFunc);
    }


    sealed class InstrumentState<TAggregator> : InstrumentState
        where TAggregator : Aggregator
    {
        AggregatorStore<TAggregator> _aggregatorStore;

        public InstrumentState(LabelAggregation labelConfig, Func<TAggregator> createAggregatorFunc)
        {
            _aggregatorStore = new AggregatorStore<TAggregator>(labelConfig, createAggregatorFunc);
        }

        public override void Collect(MeterInstrument instrument, Action<LabeledAggregationStatistics> aggregationVisitFunc)
        {
            _aggregatorStore.Collect(aggregationVisitFunc);
        }

        public override void Update(double measurement, ReadOnlySpan<(string LabelName, string LabelValue)> labels)
        {
            // TODO: we need to figure out our atomicity guarantees. If this function updates
            // multiple aggregators for a single measurement or any aggregator
            // has state spread across multiple fields then we could see torn state. Other threads
            // might be updating or reading those values concurrently. At present this code is not
            // thread-safe.
            TAggregator aggregator = _aggregatorStore.GetAggregator(labels);
            aggregator?.Update(measurement);
        }
    }
}
