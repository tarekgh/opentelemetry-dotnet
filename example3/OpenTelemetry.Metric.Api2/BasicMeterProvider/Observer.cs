using System.Collections.Generic;

namespace OpenTelemetry.Metric.Api2
{
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
}
