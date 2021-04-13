using System;
using System.Collections.Generic;
using System.Diagnostics;
using SquidLibrary;

namespace ConsoleApp6
{
    class Program
    {
        static void Main(string[] args)
        {
            // Do whatever configuration OT SDK wants to do
            // this sets the default SDK, sets exporters, determines which metrics to listen to,
            // maybe registers specializations or modifications in how the data is emitted
            // OpenTelemetry.Configure(...)

            Run();
        }

        static void Run()
        {
            SquidAnalyzer worker = new SquidAnalyzer();
            worker.ProcessSquid("squid1");
            worker.ProcessSquid("squid2");
        }
    }
}
