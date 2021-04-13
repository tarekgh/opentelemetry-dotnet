using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenTelemetry.Metric.Sdk;

namespace Microsoft.OpenTelemetry.Export
{
    public class OTLPExporter : Exporter
    {
        private Task exportTask;
        private ConcurrentQueue<ExportItem> queue = new();
        private int periodMilli;
        private int batchSize;

        private ProtoBufClient client = new();

        private ConcurrentQueue<byte[]> cloud = new();
        private Task receiveTask;
        private CancellationTokenSource receiveTokenSrc = new();

        public OTLPExporter(int batchSize, int periodMilli)
        {
            this.batchSize = batchSize;
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
            // TODO
        }

        public override void Start(CancellationToken token)
        {
            exportTask = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(this.periodMilli, token);
                    }
                    catch (TaskCanceledException)
                    {
                        // Do Nothing
                    }

                    Process();
                }
            });

            receiveTask = Task.Run(async () => await ReceiveTask(receiveTokenSrc.Token));
        }

        public override void Stop()
        {
            if (exportTask is not null)
            {
                exportTask.Wait();
            }

            receiveTokenSrc.Cancel();
            if (receiveTask is not null)
            {
                receiveTask.Wait();
            }
        }

        public void Process()
        {
            var que = Interlocked.Exchange(ref queue, new ConcurrentQueue<ExportItem>());

            // Batch it up

            var items = new List<ExportItem>();
            while (que.TryDequeue(out var item))
            {
                items.Add(item);

                if (items.Count >= this.batchSize)
                {
                    Console.WriteLine($"OTLP Exporter sends {items.Count} items...");
                    var bytes = client.Send(items.ToArray());
                    cloud.Enqueue(bytes);
                    items.Clear();
                }
            }

            if (items.Count > 0)
            {
                Console.WriteLine($"OTLP Exporter sends {items.Count} items...");
                var bytes = client.Send(items.ToArray());
                cloud.Enqueue(bytes);
            }
        }

        public async Task ReceiveTask(CancellationToken token)
        {
            while (!token.IsCancellationRequested || !cloud.IsEmpty)
            {
                while (cloud.TryDequeue(out var bytes))
                {
                    client.Receive(bytes);
                }

                await Task.Delay(100);
            }
        }
    }
}
