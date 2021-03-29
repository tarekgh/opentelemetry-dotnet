using System;
using System.Reflection;
using Microsoft.Diagnostics.Metric;

namespace OpenTelemetry.Metric.Api
{
    public interface IMeterProvider
    {
        Meter GetMeter(string name, string version);
        Meter GetMeter<T>();
    }

    public class MeterProvider : IMeterProvider
    {
        public static IMeterProvider Global { get; private set; } = new MeterProvider();

        public static IMeterProvider SetMeterProvider(IMeterProvider provider)
        {
            IMeterProvider oldProvider = Global;
            Global = provider;
            return oldProvider;
        }

        public MeterProvider()
        {
        }

        public Meter GetMeter(string name, string version)
        {
            return new Meter(name, version);
        }

        public Meter GetMeter<T>()
        {
            var clazzType = typeof(T);
            Assembly asm = clazzType.Assembly;

            string name = clazzType.FullName;
            var asmVersion = asm.GetName().Version?.ToString();
            var fileVersion = asm.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
            var productVersion = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

            var version = productVersion ?? asmVersion ?? fileVersion ?? "";

            return new Meter(name, version);
        }
    }
}
