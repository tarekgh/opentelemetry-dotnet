using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ConsoleApp6
{
    public class MetricListener
    {
        public Func<MetricSource, bool> ShouldListenTo { get; set; }
        public Action<Metric> MetricCreated { get; set; }
        public Action<Metric, int, LabelSet> MeasurementRecorded { get; set; }
    }
}
