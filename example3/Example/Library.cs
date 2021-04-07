using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Metric;

namespace MyLibrary
{
    public class Library
    {
        static Meter s_meter = new Meter("Library");
        static Counter s_c1 = s_meter.CreateCounter("c1");

        Counter counter_request;
        Counter counter_request2;
        Counter counter_request3;
        Gauge gauge_qsize;
        int count = 0;
        string _name;

        public Library(string name, CancellationToken token)
        {
            _name = name;

            counter_request = s_meter.CreateCounter("request2");

            gauge_qsize = s_meter.CreateGauge("queue_size");

            //TODO: make this async
            counter_request3 = s_meter.CreateCounter("request3");

            counter_request2 = s_meter.CreateCounter("requests");

            var counter_registered = s_meter.CreateCounter("registered");
            counter_registered.Add(1, ("Program","test"), ("LibraryInstanceName", name));
        }

        public void DoOperation()
        {

            s_c1.Add(5, ("Label1", _name), ("Label2", "Tomato"));

            // Example of recording 1 measurment

            var opernum = count % 3;

            //var labels = new MetricLabelSet(("OperNum", $"{opernum}"));

            
            counter_request2.Add(1);

            counter_request2.Add(0.15, ("OpenNum", opernum.ToString()));

            //counter_request3.Observe();


            //I am proposing there is no batching API
            /*
            // Example of recording a batch of measurements

            var labels2 = new MetricLabelSet(
                ("OperNum", $"{opernum}"),
                ("Mode", "Batch"));

            
            new BatchMetricBuilder(labels2)
                .RecordMetric(counter_request, 1.0)
                .RecordMetric(gauge_qsize, count)
                .RecordMetric(counter_request3, 1)
                .RecordMetric(counter_request3, 0.1)
                .Record();
            */

            count++;
        }
    }
}
