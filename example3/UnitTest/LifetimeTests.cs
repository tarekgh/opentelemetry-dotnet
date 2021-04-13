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
            Counter c = m.CreateCounter("C");
            using TestListener listener = new TestListener("TestMeterA", false);

            Assert.Null(listener.LastPublish);
            listener.Start();
            Assert.Equal(c, listener.LastPublish);
        }

        [Fact]
        public void PublishMeterFirstLateCounter()
        {
            using Meter m = new Meter("TestMeterB");
            using TestListener listener = new TestListener("TestMeterB", false);

            Assert.Null(listener.LastPublish);
            listener.Start();
            Assert.Null(listener.LastPublish);
            Counter c = m.CreateCounter("C");
            Assert.Equal(c, listener.LastPublish);
        }

        [Fact]
        public void PublishListenerFirst()
        {
            using TestListener listener = new TestListener("TestMeterC", false);

            Assert.Null(listener.LastPublish);
            listener.Start();
            Assert.Null(listener.LastPublish);
            using Meter m = new Meter("TestMeterC");
            Assert.Null(listener.LastPublish);
            Counter c = m.CreateCounter("C");
            Assert.Equal(c, listener.LastPublish);
        }

        [Fact]
        public void UnpublishOnMeterDispose()
        {
            using Meter m = new Meter("TestMeterD");
            Counter c = m.CreateCounter("C");
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
            Counter c = m.CreateCounter("C");
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
            Counter c = m.CreateCounter("C");
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
            Counter c = m.CreateCounter("C");
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
            Counter c = m.CreateCounter("C");
            Assert.Null(listener.LastPublish);
        }

        [Fact]
        public void UnpublishListenerDispose()
        {
            using Meter m = new Meter("TestMeterH");
            Counter c = m.CreateCounter("C");
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
            Counter c = m.CreateCounter("C");
            using TestListener listener = new TestListener("TestMeterI", true);

            listener.Start();
            Assert.Null(listener.LastUnpublish);
            listener.Dispose();
            Assert.Equal(c, listener.LastUnpublish);
            listener.LastUnpublish = null;
            listener.Dispose();
            Assert.Null(listener.LastUnpublish);
        }
    }

    class TestListener : MeterInstrumentListener
    {
        public MeterInstrument LastPublish { get; set; }
        public MeterInstrument LastUnpublish { get; set; }
        bool _subscribe;

        string _meterName;
        public TestListener(string meterName, bool subscribe) { _meterName = meterName; _subscribe = subscribe; }
        protected override void MeterInstrumentPublished(MeterInstrument instrument, MeterSubscribeOptions subscribeOptions)
        {
            if (instrument.Meter.Name != _meterName) return;

            LastPublish = instrument;
            if (_subscribe)
            {
                subscribeOptions.Subscribe();
            }
        }

        protected override void MeterInstrumentUnpublished(MeterInstrument instrument, object cookie)
        {
            Assert.Equal(_meterName, instrument.Meter.Name);
            LastUnpublish = instrument;
        }
    }
}
