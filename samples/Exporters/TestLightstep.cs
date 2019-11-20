﻿// <auto-generated/>

using OpenTelemetry.Trace.Configuration;

namespace Samples
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using OpenTelemetry.Exporter.LightStep;
    using OpenTelemetry.Trace;
    using OpenTelemetry.Trace.Export;

    internal class TestLightstep
    {
        internal static object Run(string accessToken)
        {
            var exporter = new LightStepTraceExporter(
                new LightStepTraceExporterOptions
                {
                    AccessToken = accessToken,
                    ServiceName = "lightstep-test",
                });

            // Create a tracer. 
            using (var tracerFactory = TracerFactory.Create(builder => builder.AddProcessorPipeline(c => c.SetExporter(exporter))))
            {
                var tracer = tracerFactory.GetTracer("lightstep-test");
                using (tracer.StartActiveSpan("Main", out var span))
                {
                    span.SetAttribute("custom-attribute", 55);
                    Console.WriteLine("About to do a busy work");
                    for (int i = 0; i < 10; i++)
                    {
                        DoWork(i, tracer);
                    }
                }

                Thread.Sleep(10000);
                return null;
            }
        }
        
        private static void DoWork(int i, ITracer tracer)
        {
            using (tracer.WithSpan(tracer.StartSpan("DoWork")))
            {
                // Simulate some work.
                var span = tracer.CurrentSpan;

                try
                {
                    Console.WriteLine("Doing busy work");
                    Thread.Sleep(1000);
                }
                catch (ArgumentOutOfRangeException e)
                {
                    // Set status upon error
                    span.Status = Status.Internal.WithDescription(e.ToString());
                }

                // Annotate our span to capture metadata about our operation
                var attributes = new Dictionary<string, object>();
                attributes.Add("use", "demo");
                span.AddEvent("Invoking DoWork", attributes);
            }
        }
    }
}