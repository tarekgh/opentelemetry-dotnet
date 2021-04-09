using System.Collections.Generic;

namespace OpenTelemetry.Metric.Api2
{
    public abstract class Observer
    {
    }

    public class Observer<T> : Observer
    {
        internal List<(T value, (string name, object value)[] attributes)> measures = new();

        public void Observe(T value, params (string name, object value)[] attributes)
        {
            measures.Add((value, attributes));
        }
    }
}
