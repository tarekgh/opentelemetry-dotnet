using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Metric;

namespace OpenTelemetry.Metric.Sdk
{
    public class InstrumentBuilder
    {
        InstrumentAggregationBuilder _defaultAggBuilder;

        public InstrumentBuilder(MeterInstrument instrument)
        {
            Name = instrument.Name;
            _defaultAggBuilder = new InstrumentAggregationBuilder(instrument);
        }

        public string Name { get; set; }
        public MeasurementAggregation MeasurementAggregation
        {
            get { return _defaultAggBuilder.MeasurementAggregation; }
            set { _defaultAggBuilder.MeasurementAggregation = value; }
        }

        public InstrumentBuilder ExcludeAllLabels()
        {
            _defaultAggBuilder.ExcludeAllLabels();
            return this;
        }
        public InstrumentBuilder IncludeAllLabels()
        {
            _defaultAggBuilder.IncludeAllLabels();
            return this;
        }
        public InstrumentBuilder IncludeLabel(string labelName)
        {
            _defaultAggBuilder.IncludeLabel(labelName);
            return this;
        }
        public InstrumentBuilder ExcludeLabel(string labelName)
        {
            _defaultAggBuilder.ExcludeLabel(labelName);
            return this;
        }
        public InstrumentBuilder IncludeLabel(string labelName, Func<string, string> labelValueFunc)
        {
            _defaultAggBuilder.IncludeLabel(labelName, labelValueFunc);
            return this;
        }
            
        public InstrumentBuilder IncludeAmbientLabel(string labelName, Func<string> labelValueFunc)
        {
            _defaultAggBuilder.IncludeAmbientLabel(labelName, labelValueFunc);
            return this;
        }
            

        internal InstrumentState Build()
        {
            InstrumentAggregation defaultAgg = _defaultAggBuilder.Build();
            Type aggregatorType = GetAggregatorType(defaultAgg.MeasurementAggregation);
            Type instrumentStateType = typeof(InstrumentState<>).MakeGenericType(aggregatorType);
            return (InstrumentState)Activator.CreateInstance(instrumentStateType, defaultAgg.LabelAggregation);
        }

        static Type GetAggregatorType(MeasurementAggregation config)
        {
            if (config is SumAggregation)
            {
                return typeof(SumCountMinMax);
            }
            else if (config is LastValueAggregation)
            {
                return typeof(LastValue);
            }
            else
            {
                // for any unsupported aggregations this SDK converts it to SumCountMinMax
                // this is a flexible policy we can make it do whatever we want
                return typeof(SumCountMinMax);
            }
        }
    }

    class InstrumentAggregationBuilder
    {
        LabelAggregationBuilder _labelAggBuilder = new LabelAggregationBuilder();

        public InstrumentAggregationBuilder(MeterInstrument instrument)
        {
            MeasurementAggregation = GetDefaultMeasurementAggregation(instrument);
        }

        public MeasurementAggregation MeasurementAggregation { get; set; }

        public void ExcludeAllLabels() => _labelAggBuilder.ExcludeAllLabels();
        public void IncludeAllLabels() => _labelAggBuilder.IncludeAllLabels();
        public void IncludeLabel(string labelName) => _labelAggBuilder.IncludeLabel(labelName);
        public void ExcludeLabel(string labelName) => _labelAggBuilder.ExcludeLabel(labelName);
        public void IncludeLabel(string labelName, Func<string, string> labelValueFunc) =>
            _labelAggBuilder.IncludeLabel(labelName, labelValueFunc);
        public void IncludeAmbientLabel(string labelName, Func<string> labelValueFunc) =>
            _labelAggBuilder.IncludeAmbientLabel(labelName, labelValueFunc);

        internal InstrumentAggregation Build()
        {
            return new InstrumentAggregation(MeasurementAggregation, _labelAggBuilder.Build());
        }

        static MeasurementAggregation GetDefaultMeasurementAggregation(MeterInstrument instrument)
        {
            // In the future instruments will likely have a more explicit default aggregation configuration API
            // but for now the type of the instrument implies the config
            //
            if (instrument is Counter)
            {
                return MeasurementAggregations.Sum;
            }
            else if (instrument is CounterFunc)
            {
                return MeasurementAggregations.Sum;
            }
            else if (instrument is Gauge)
            {
                return MeasurementAggregations.LastValue;
            }
            else
            {
                // TODO: decide how to handle unknown instrument types
                // This could be an error, drop the data silently, or handle it
                // in some default way
                return null;
            }
        }
    }


    class LabelAggregationBuilder
    {
        public bool IncludeAllCallsiteLabels = true;
        public Dictionary<string, LabelConfiguration> LabelConfigs = new Dictionary<string, LabelConfiguration>();

        public void IncludeLabel(string labelName)
        {
            if (!LabelConfigs.TryGetValue(labelName, out LabelConfiguration lc))
            {
                lc = new LabelConfiguration() { LabelName = labelName };
                LabelConfigs.Add(labelName, lc);
            }
            lc.IsExclusion = false;
            lc.RequiresSourceLabel = true;
            lc.ComputeLabelValue = null;
        }

        public void ExcludeLabel(string labelName)
        {
            if (!LabelConfigs.TryGetValue(labelName, out LabelConfiguration lc))
            {
                lc = new LabelConfiguration() { LabelName = labelName };
                LabelConfigs.Add(labelName, lc);
            }
            lc.IsExclusion = true;
            lc.RequiresSourceLabel = false;
            lc.ComputeLabelValue = null;
        }

        public void ExcludeAllLabels()
        {
            IncludeAllCallsiteLabels = false;
            LabelConfigs.Clear();
        }

        public void IncludeAllLabels()
        {
            IncludeAllCallsiteLabels = true;
            foreach(string key in LabelConfigs.Keys.ToArray())
            {
                if(LabelConfigs[key].IsExclusion)
                {
                    LabelConfigs.Remove(key);
                }
            }
        }

        public void IncludeLabel(string labelName, Func<string, string> mapLabelValue)
        {
            if (!LabelConfigs.TryGetValue(labelName, out LabelConfiguration lc))
            {
                lc = new LabelConfiguration() { LabelName = labelName };
                LabelConfigs.Add(labelName, lc);
            }
            lc.IsExclusion = false;
            lc.RequiresSourceLabel = true;
            lc.ComputeLabelValue = mapLabelValue;
        }

        public void IncludeAmbientLabel(string labelName, Func<string> calculateLabelValue)
        {
            if (!LabelConfigs.TryGetValue(labelName, out LabelConfiguration lc))
            {
                lc = new LabelConfiguration() { LabelName = labelName };
                LabelConfigs.Add(labelName, lc);
            }
            lc.IsExclusion = false;
            lc.RequiresSourceLabel = false;
            lc.ComputeLabelValue = _ => calculateLabelValue();
        }

        internal LabelAggregation Build()
        {
            LabelMapping[] requiredLabels = LabelConfigs.Values
                .Where(lc => !lc.IsExclusion)
                .Select(lc => new LabelMapping(lc.LabelName, lc.RequiresSourceLabel, lc.ComputeLabelValue))
                .ToArray();
            string[] excludedLabels = LabelConfigs.Values
                .Where(lc => lc.IsExclusion)
                .Select(lc => lc.LabelName)
                .ToArray();
            return new LabelAggregation(IncludeAllCallsiteLabels, requiredLabels, excludedLabels);
        }
    }

    class LabelConfiguration
    {
        public bool IsExclusion;
        public bool RequiresSourceLabel;
        public string LabelName;
        public Func<string, string> ComputeLabelValue;
    }
}
