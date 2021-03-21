using System.Collections.Generic;
using Microsoft.Diagnostics.Metric;

namespace OpenTelemetry.Metric.Api
{
    public interface IMeter
    {
        Counter CreateCounter(string name);
        Counter CreateCounter(string name, params string[] dimNames);
        Counter CreateCounter(string name, Dictionary<string,string> labels);
        Counter CreateCounter(string name, Dictionary<string,string> labels, params string[] dimNames);

        /*
        Counter1D<T> CreateCounter<T>(string name, string dn1);
        Counter1D<T> CreateCounter<T>(string name, Dictionary<string,string> labels, string dn1);

        Counter2D<T> CreateCounter<T>(string name, string dn1, string dn2);
        Counter2D<T> CreateCounter<T>(string name, Dictionary<string,string> labels, string dn1, string dn2);

        Counter3D<T> CreateCounter<T>(string name, string dn1, string dn2, string dn3);
        Counter3D<T> CreateCounter<T>(string name, Dictionary<string,string> labels, string dn1, string dn2, string dn3);
        */
        Gauge CreateGauge(string name);
        Gauge CreateGauge(string name, Dictionary<string,string> labels);
        Gauge CreateGauge(string name, params string[] dimNames);
        Gauge CreateGauge(string name, Dictionary<string,string> labels, params string[] dimNames);
    }

    public class DotNetMeter : IMeter
    {
        Meter _meter;

        public DotNetMeter(string libname, string libver)
        {
            _meter = new Meter(libname, libver);
        }

        public Counter CreateCounter(string name)
        {
            return new Counter(name, _meter);
        }

        public Counter CreateCounter(string name, params string[] dimNames)
        {
            return new Counter(name, dimNames, _meter);
        }

        public Counter CreateCounter(string name, Dictionary<string,string> labels)
        {
            return new Counter(name, labels, _meter);
        }

        public Counter CreateCounter(string name, Dictionary<string,string> labels, string[] dimNames)
        {
            return new Counter(name, labels, dimNames, _meter);
        }

        /* Including the dimension eliminates mistakes with wrong number of params but wrong order
         * is still possible which Bogdan was also worried about. I'm going to experiment with
         * stronger typing to address that concern

        public Counter1D<T> CreateCounter<T>(string name, string d1)
        {
            return new Counter1D<T>(name, d1);
        }

        public Counter1D<T> CreateCounter<T>(string name, Dictionary<string,string> labels, string d1)
        {
            return new Counter1D<T>(libname, libver, name, labels, d1);
        }

        public Counter2D<T> CreateCounter<T>(string name, string d1, string d2)
        {
            return new Counter2D<T>(libname, libver, name, d1, d2);
        }

        public Counter2D<T> CreateCounter<T>(string name, Dictionary<string,string> labels, string d1, string d2)
        {
            return new Counter2D<T>(libname, libver, name, labels, d1, d2);
        }

        public Counter3D<T> CreateCounter<T>(string name, string d1, string d2, string d3)
        {
            return new Counter3D<T>(libname, libver, name, d1, d2, d3);
        }

        public Counter3D<T> CreateCounter<T>(string name, Dictionary<string,string> labels, string d1, string d2, string d3)
        {
            return new Counter3D<T>(libname, libver, name, labels, d1, d2, d3);
        }*/

        public Gauge CreateGauge(string name)
        {
            return new Gauge(name, _meter);
        }

        public Gauge CreateGauge(string name, Dictionary<string,string> labels)
        {
            return new Gauge(name, labels, _meter);
        }

        public Gauge CreateGauge(string name, params string[] dimNames)
        {
            return new Gauge(name, dimNames, _meter);
        }

        public Gauge CreateGauge(string name, Dictionary<string,string> labels, params string[] dimNames)
        {
            return new Gauge(name, labels, dimNames, _meter);
        }
    }

    /*
    public class Counter1D<T>
    {
        Counter counter;

        public Counter1D(string libname, string libver, string name, string d1)
        {
            counter = new Counter(libname, libver, name, new string[] { d1 });
        }

        public Counter1D(string libname, string libver, string name, Dictionary<string,string> labels, string d1)
        {
            counter = new Counter(libname, libver, name, labels, new string[] { d1 });
        }

        public void Add(T v, string dv1)
        {
            double val = 0;
            if (v is int iv)
            {
                val = iv;
            }
            else if (v is double dv)
            {
                val = dv;
            }

            counter.Add(val, new string[] { dv1 });
        }
    }

    public class Counter2D<T>
    {
        Counter counter;

        public Counter2D(string libname, string libver, string name, string d1, string d2)
        {
            counter = new Counter(libname, libver, name, new string[] { d1, d2 });
        }

        public Counter2D(string libname, string libver, string name, Dictionary<string,string> labels, string d1, string d2)
        {
            counter = new Counter(libname, libver, name, labels, new string[] { d1, d2 });
        }

        public void Add(T v, string dv1, string dv2)
        {
            double val = 0;
            if (v is int iv)
            {
                val = iv;
            }
            else if (v is double dv)
            {
                val = dv;
            }

            counter.Add(val, new string[] { dv1, dv2 });
        }
    }

    public class Counter3D<T>
    {
        Counter counter;

        public Counter3D(string libname, string libver, string name, string d1, string d2, string d3)
        {
            counter = new Counter(libname, libver, name, new string[] { d1, d2, d3 });
        }

        public Counter3D(string libname, string libver, string name, Dictionary<string,string> labels, string d1, string d2, string d3)
        {
            counter = new Counter(libname, libver, name, labels, new string[] { d1, d2, d3 });
        }

        public void Add(T v, string dv1, string dv2, string dv3)
        {
            double val = 0;
            if (v is int iv)
            {
                val = iv;
            }
            else if (v is double dv)
            {
                val = dv;
            }

            counter.Add(val, new string[] { dv1, dv2, dv3 });
        }
    }*/
}
