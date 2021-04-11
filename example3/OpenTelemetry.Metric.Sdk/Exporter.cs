using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Metric;

namespace OpenTelemetry.Metric.Sdk
{
    public abstract class Exporter
    {
        public abstract void Export(ExportItem[] exports);
        public abstract void BeginFlush();

        public abstract void Start(CancellationToken token);

        public abstract void Stop();
    }

    public class ExportItem
    {
        public DateTimeOffset dt { get; set; }
        public string MeterName { get; set; }
        public string MeterVersion { get; set; }
        public string InstrumentName { get; set; }
        public MetricLabelSet Labels { get; set; }
        public MeasurementAggregation MeasurementAggregation { get; set; }
        public (string name, string value)[] AggData { get; set; }
    }
}
