using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpenTelemetry.Metric.Sdk;

namespace Microsoft.OpenTelemetry.Export
{
    public class PrometheusExporter : Exporter
    {
        Task _handleRequests;
        HttpListener _listener = new HttpListener();
        CancellationTokenSource _cts;
        ExportItem[] _metrics;

        public override void BeginFlush()
        {
        }

        public override void Export(ExportItem[] exports)
        {
            lock (this)
            {
                _metrics = exports;
            }
        }

        public override void Start(CancellationToken token)
        {
            _listener.Prefixes.Add("http://localhost:9000/metrics/");
            _listener.Start();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            _handleRequests = Task.Factory.StartNew(HandleRequests, _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public override void Stop()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _listener.Stop();
                _handleRequests.Wait();
                _cts.Dispose();
            }
        }

        private void HandleRequests()
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    HttpListenerContext context = _listener.GetContext();
                    HttpListenerRequest request = context.Request;
                    HttpListenerResponse response = context.Response;
                    response.ContentType = "text/plain; version=0.0.4; charset=utf-8";
                    response.StatusCode = 200;
                    StringBuilder builder = new StringBuilder();
                    SerializeMetrics(builder);
                    byte[] buffer = Encoding.UTF8.GetBytes(builder.ToString());
                    response.ContentLength64 = buffer.Length;
                    Stream output = response.OutputStream;
                    output.Write(buffer, 0, buffer.Length);
                    output.Close();
                }
                catch (HttpListenerException) { }
            }
        }

        void SerializeMetrics(StringBuilder builder)
        {
            ExportItem[] metrics = null;
            lock (this)
            {
                metrics = _metrics;
                if(metrics == null)
                {
                    return;
                }
            }
            foreach (ExportItem e in metrics)
            {
                
                IEnumerable<(string name, string value)> labels = e.Labels.Labels;
                if (e.AggregationStatistics is DistributionStatistics dStats)
                {
                    foreach (QuantileValue qv in dStats.Quantiles)
                    {
                        string label = FormatLabels(labels.Append(("quantile", qv.Quantile.ToString())));
                        builder.Append($"{e.InstrumentName}{label} {qv.Value}\n");
                    }
                }
                else
                {
                    string label = FormatLabels(labels);
                    foreach (var (statName, val) in e.AggregationStatistics.Statistics)
                    {
                        builder.Append($"{e.InstrumentName}_{statName}{label} {val}\n");
                    }
                }
            }
        }

        private static string FormatLabels(IEnumerable<(string name, string value)> labels)
        {
            StringBuilder labelBuilder = new StringBuilder();
            if (labels.Any())
            {
                labelBuilder.Append("{");
                labelBuilder.Append(string.Join(",", labels.Select(((string name, string value) label) => $"{label.name}=\"{label.value}\"")));
                labelBuilder.Append("}");
            }
            string label = labelBuilder.ToString();
            return label;
        }
    }
}
