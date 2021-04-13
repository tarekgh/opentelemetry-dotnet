using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApp6
{
    class Sdk
    {

        MetricListener _listener;
        Task _publishMetrics;
        CancellationTokenSource _cts;
        TimeSpan _publishInterval = TimeSpan.FromSeconds(10);

        Dictionary<Tuple<>>


        public void Start()
        {
            RegisterListener();
            _cts = new CancellationTokenSource();
        }

        public void Stop()
        {
            _cts.Cancel();
            _publishMetrics.Wait();
        }

        public void RegisterListener()
        {

        }

        public async Task PublishMetrics(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(_publishInterval);
            }
        }
    }

    struct LastValueAggregate
    {
        int _lastValue;

        void Record(int newMeasurement) { _lastValue = newMeasurement; }
        public int Value => _lastValue;
    }

    struct SumAggregate
    {
        int _sum;

        void Record(int newMeasurement) { Interlocked.Add(ref _sum, newMeasurement }
        public int Value => _sum;
    }
}
