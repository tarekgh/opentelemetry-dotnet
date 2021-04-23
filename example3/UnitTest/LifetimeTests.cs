using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Metric;
using Xunit;

namespace UnitTest
{
    public class LifetimeTests
    {
        [Fact]
        public void PublishMeterFirst()
        {
            using Meter m = new Meter("TestMeterA");
            Counter<double> c = m.CreateCounter<double>("C");

            bool tested = false;

            MeterListener listener = new MeterListener()
            {
                ShouldListenTo = (meter) => meter.Name == m.Name,
                InstrumentEncountered = (instrument) => { Assert.Same(c, instrument); tested = true; }
            };

            Assert.False(tested);
            Meter.AddListener(listener);
            Assert.True(tested);
        }

        [Fact]
        public void PublishListenerFirst()
        {
            MeterInstrument c = null;
            bool tested = false;

            MeterListener listener = new MeterListener()
            {
                ShouldListenTo = (meter) => meter.Name == "TestMeterB",
                InstrumentEncountered = (instrument) => { Assert.Same(c, instrument); tested = true; }
            };

            Meter.AddListener(listener);

            using Meter m = new Meter("TestMeterB");

            Assert.False(tested);

            c = m.CreateCounter<double>("C");

            Assert.True(tested);
        }
/*
        [Fact]
        public void UnpublishOnMeterDispose()
        {
            using Meter m = new Meter("TestMeterD");
            Counter<double> c = m.CreateCounter<double>("C");
            using TestListener listener = new TestListener("TestMeterD", true);

            listener.Start();
            Assert.Null(listener.LastUnpublish);
            m.Dispose();
            Assert.Equal(c, listener.LastUnpublish);
        }

        [Fact]
        public void NoUnpublishOnDoubleDispose()
        {
            using Meter m = new Meter("TestMeterD2");
            Counter<double> c = m.CreateCounter<double>("C");
            using TestListener listener = new TestListener("TestMeterD2", true);

            listener.Start();
            Assert.Null(listener.LastUnpublish);
            m.Dispose();
            Assert.Equal(c, listener.LastUnpublish);
            listener.LastUnpublish = null;
            m.Dispose();
            Assert.Null(listener.LastUnpublish);
        }

        [Fact]
        public void UnpublishOnlyWhenSubscribed()
        {
            using Meter m = new Meter("TestMeterE");
            Counter<double> c = m.CreateCounter<double>("C");
            using TestListener listener = new TestListener("TestMeterE", false);

            listener.Start();
            Assert.Null(listener.LastUnpublish);
            m.Dispose();
            Assert.Null(listener.LastUnpublish);
        }

        [Fact]
        public void NoPublishAfterMeterDispose()
        {
            using Meter m = new Meter("TestMeterF");
            Counter<double> c = m.CreateCounter<double>("C");
            m.Dispose();
            using TestListener listener = new TestListener("TestMeterF", false);

            Assert.Null(listener.LastPublish);
            listener.Start();
            Assert.Null(listener.LastPublish);
        }

        [Fact]
        public void NoPublishAfterListenerDispose()
        {
            using Meter m = new Meter("TestMeterG");
            using TestListener listener = new TestListener("TestMeterG", false);

            Assert.Null(listener.LastPublish);
            listener.Start();
            Assert.Null(listener.LastPublish);
            listener.Dispose();
            Assert.Null(listener.LastPublish);
            Counter<double> c = m.CreateCounter<double>("C");
            Assert.Null(listener.LastPublish);
        }

        [Fact]
        public void UnpublishListenerDispose()
        {
            using Meter m = new Meter("TestMeterH");
            Counter<double> c = m.CreateCounter<double>("C");
            using TestListener listener = new TestListener("TestMeterH", true);

            listener.Start();
            Assert.Null(listener.LastUnpublish);
            listener.Dispose();
            Assert.Equal(c, listener.LastUnpublish);
        }

        [Fact]
        public void NoUnpublishListenerDoubleDispose()
        {
            using Meter m = new Meter("TestMeterI");
            Counter<double> c = m.CreateCounter<double>("C");
            using TestListener listener = new TestListener("TestMeterI", true);

            listener.Start();
            Assert.Null(listener.LastUnpublish);
            listener.Dispose();
            Assert.Equal(c, listener.LastUnpublish);
            listener.LastUnpublish = null;
            listener.Dispose();
            Assert.Null(listener.LastUnpublish);
        }
*/
    }
}
