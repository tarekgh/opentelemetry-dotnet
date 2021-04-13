using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenTelemetry.Metric.Sdk
{

    interface ILabelSet
    {
        IEnumerable<(string LabelName, string LabelValue)> Labels { get; }
    }

    struct LabelSet1 : ILabelSet, IEquatable<LabelSet1>
    {
        (string LabelName, string LabelValue) _label1;

        public LabelSet1((string LabelName, string LabelValue) label)
        {
            _label1 = label;
        }

        public LabelSet1(string labelName, string labelValue)
        {
            _label1 = (labelName, labelValue);
        }

        public IEnumerable<(string LabelName, string LabelValue)> Labels =>
            new (string, string)[] { _label1 };

        public bool Equals(LabelSet1 other)
        {
            return _label1 == other._label1;
        }

        public override int GetHashCode()
        {
            return _label1.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return obj is LabelSet1 && Equals((LabelSet1)obj);
        }
    }

    struct LabelSet2 : ILabelSet, IEquatable<LabelSet2>
    {
        (string LabelName, string LabelValue) _label1;
        (string LabelName, string LabelValue) _label2;

        public LabelSet2(
            (string LabelName, string LabelValue) label1,
            (string LabelName, string LabelValue) label2)
        {
            _label1 = label1;
            _label2 = label2;
        }

        public LabelSet2(string labelName1, string labelValue1,
            string labelName2, string labelValue2)
        {
            _label1 = (labelName1, labelValue1);
            _label2 = (labelName2, labelValue2);
        }

        public IEnumerable<(string LabelName, string LabelValue)> Labels =>
            new (string, string)[] { _label1, _label2 };

        public bool Equals(LabelSet2 other)
        {
            return _label1 == other._label1 && _label2 == other._label2;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_label1.LabelValue.GetHashCode(), _label2.LabelValue.GetHashCode());
        }

        public override bool Equals(object obj)
        {
            return obj is LabelSet2 && Equals((LabelSet2)obj);
        }
    }

    public class MetricLabelSet : ILabelSet, IEquatable<MetricLabelSet>
    {
        static private (string name, string value)[] emptyLabel = { };

        static private MetricLabelSet defaultLabel = new MetricLabelSet();

        private (string name, string value)[] labels = { };

        static public MetricLabelSet DefaultLabelSet
        {
            get => defaultLabel;
        }

        public MetricLabelSet()
        {
            labels = emptyLabel;
        }

        public MetricLabelSet(string[] labelNames, string[] labelValues)
        {
            this.labels = new (string, string)[labelNames.Length];
            for (int i = 0; i < labelNames.Length; i++)
            {
                if (i < labelValues.Length)
                {
                    this.labels[i] = (labelNames[i], labelValues[i]);
                }
                else
                {
                    this.labels[i] = (labelNames[i], "");
                }
            }
        }

        public MetricLabelSet(params (string name, string value)[] labels)
        {
            this.labels = labels;
        }

        public MetricLabelSet(IEnumerable<(string name, string value)> labels)
        {
            this.labels = labels.ToArray();
        }

        public IEnumerable<(string LabelName, string LabelValue)> Labels => labels;

        /// <summary>
        /// Return Array of Tuple&lt;Key,Value&gt;.
        /// </summary>
        public virtual (string name, string value)[] GetLabels()
        {
            return this.labels;
        }

        public bool Equals(MetricLabelSet other)
        {
            if (this.labels.Length != other.labels.Length)
            {
                return false;
            }

            var len = this.labels.Length;
            for (var i = 0; i < len; i++)
            {
                if (this.labels[i].name != other.labels[i].name)
                {
                    return false;
                }

                if (this.labels[i].value != other.labels[i].value)
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(Object obj)
        {
            if (obj is MetricLabelSet other)
            {
                return this.Equals(other);
            }

            return false;
        }

        public override int GetHashCode()
        {
            HashCode hash = new HashCode();

            foreach (var l in labels)
            {
                hash.Add(l.name);
                hash.Add(l.value);
            }

            return hash.ToHashCode();
        }

        public override string ToString()
        {
            var items = labels.Select(k => $"{k.name}={k.value}");
            return String.Join(";", items);
        }
    }
}
