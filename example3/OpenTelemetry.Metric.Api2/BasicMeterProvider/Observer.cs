using System.Collections.Generic;

namespace OpenTelemetry.Metric.Api2
{
    public class Observer<T>
    {
        internal List<(T value, (string name, object value)[] attributes)> Measures { get; } = new ();

        public void Observe(T value, params (string name, object value)[] attributes)
        {
            this.Measures.Add((value, attributes));
        }
    }
}
