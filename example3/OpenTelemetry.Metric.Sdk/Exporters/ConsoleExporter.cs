using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace OpenTelemetry.Metric.Sdk
{
    public class ConsoleExporter : Exporter
    {
        private Task exportTask;
        private ConcurrentQueue<ExportItem> queue = new();
        private string name;
        private int periodMilli;
        private bool flush;

        public ConsoleExporter(string name, int periodMilli)
        {
            this.name = name;
            this.periodMilli = periodMilli;
        }

        public override void Export(ExportItem[] exports)
        {
            foreach (var export in exports)
            {
                queue.Enqueue(export);
            }
        }

        public override void BeginFlush()
        {
            flush = true;
            Process();
        }

        public override void Start(CancellationToken token)
        {
            exportTask = Task.Run(async () => {
                while (!token.IsCancellationRequested && Process())
                {
                    try
                    {
                        await Task.Delay(this.periodMilli, token);
                    }
                    catch (TaskCanceledException)
                    {
                        // Do Nothing
                    }
                }
            });
        }

        public override void Stop()
        {
            exportTask.Wait();
        }

        public bool Process()
        {
            Console.WriteLine($"ConsoleExporter [{this.name}]...");

            var que = Interlocked.Exchange(ref queue, new ConcurrentQueue<ExportItem>());

            var sortedGroups = que.GroupBy(k => $"{k.MeterName} | {k.MeterVersion} | {k.InstrumentName}").OrderBy(g => g.Key);

            foreach (var group in sortedGroups)
            {
                Console.WriteLine(group.Key);

                var items = new List<string>();

                foreach (var q in group)
                {
                    var aggdata = q.AggData.Select(k => $"{k.name}={k.value}");
                    var dim = String.Join( " | ", q.Labels.GetLabels().Select(k => $"{k.name}={k.value}"));
                    if (dim == "")
                    {
                        dim = "{_Total}";
                    }
                    items.Add($"    {dim}{Environment.NewLine}" +
                        $"        {q.AggregationConfig.GetType().Name}: {String.Join("|", aggdata)}");
                }

                items.Sort();
                foreach (var item in items)
                {
                    Console.WriteLine(item);
                }
            }

            return !flush;
        }
    }
}
