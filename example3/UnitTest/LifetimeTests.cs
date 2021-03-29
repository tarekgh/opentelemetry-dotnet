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
            MeterInstrument publishedCounter = null;
            using MeterInstrumentListener listener = new MeterInstrumentListener()
            {
                MeterInstrumentPublished = (instrument, options) => { if (instrument.Meter == m) publishedCounter = instrument; }
            };

            Assert.Null(publishedCounter);
            listener.Start();
            Assert.Equal(c, publishedCounter);
        }

        [Fact]
        public void PublishMeterFirstLateCounter()
        {
            using Meter m = new Meter("TestMeterB");

            MeterInstrument publishedCounter = null;
            using MeterInstrumentListener listener = new MeterInstrumentListener()
            {
                MeterInstrumentPublished = (instrument, options) => { if(instrument.Meter == m) publishedCounter = instrument; }
            };

            Assert.Null(publishedCounter);
            listener.Start();
            Assert.Null(publishedCounter);
            Counter c = m.CreateCounter("C");
            Assert.Equal(c, publishedCounter);
        }

        [Fact]
        public void PublishListenerFirst()
        {
            MeterInstrument publishedCounter = null;
            using MeterInstrumentListener listener = new MeterInstrumentListener()
            {
                MeterInstrumentPublished = (instrument, options) => { if (instrument.Meter.Name == "TestMeterC") publishedCounter = instrument; }
            };

            Assert.Null(publishedCounter);
            listener.Start();
            Assert.Null(publishedCounter);
            using Meter m = new Meter("TestMeterC");
            Assert.Null(publishedCounter);
            Counter c = m.CreateCounter("C");
            Assert.Equal(c, publishedCounter);
        }

        [Fact]
        public void UnpublishOnMeterDispose()
        {
            using Meter m = new Meter("TestMeterD");
            Counter c = m.CreateCounter("C");
            MeterInstrument unpublishCounter = null;
            using MeterInstrumentListener listener = new MeterInstrumentListener()
            {
                MeterInstrumentPublished = (instrument, options) => options.Subscribe(),
                MeterInstrumentUnpublished = (instrument, cookie) => { if (instrument.Meter == m) unpublishCounter = instrument; }
            };

            listener.Start();
            Assert.Null(unpublishCounter);
            m.Dispose();
            Assert.Equal(c, unpublishCounter);
        }

        [Fact]
        public void NoUnpublishOnDoubleDispose()
        {
            using Meter m = new Meter("TestMeterD");
            Counter c = m.CreateCounter("C");
            MeterInstrument unpublishCounter = null;
            using MeterInstrumentListener listener = new MeterInstrumentListener()
            {
                MeterInstrumentPublished = (instrument, options) => { if (instrument.Meter == m) options.Subscribe(); },
                MeterInstrumentUnpublished = (instrument, cookie) => { unpublishCounter = instrument; }
            };

            listener.Start();
            Assert.Null(unpublishCounter);
            m.Dispose();
            Assert.Equal(c, unpublishCounter);
            unpublishCounter = null;
            m.Dispose();
            Assert.Null(unpublishCounter);
        }

        [Fact]
        public void UnpublishOnlyWhenSubscribed()
        {
            using Meter m = new Meter("TestMeterE");
            Counter c = m.CreateCounter("C");
            MeterInstrument unpublishCounter = null;
            using MeterInstrumentListener listener = new MeterInstrumentListener()
            {
                MeterInstrumentUnpublished = (instrument, cookie) => { unpublishCounter = instrument; }
            };

            listener.Start();
            Assert.Null(unpublishCounter);
            m.Dispose();
            Assert.Null(unpublishCounter);
        }

        [Fact]
        public void NoPublishAfterMeterDispose()
        {
            using Meter m = new Meter("TestMeterF");
            Counter c = m.CreateCounter("C");
            m.Dispose();

            MeterInstrument publishCounter = null;
            using MeterInstrumentListener listener = new MeterInstrumentListener()
            {
                MeterInstrumentPublished = (instrument, options) => { if (instrument.Meter == m) { publishCounter = instrument; } }
            };

            Assert.Null(publishCounter);
            listener.Start();
            Assert.Null(publishCounter);
        }

        [Fact]
        public void NoPublishAfterListenerDispose()
        {
            using Meter m = new Meter("TestMeterG");

            MeterInstrument publishCounter = null;
            using MeterInstrumentListener listener = new MeterInstrumentListener()
            {
                MeterInstrumentPublished = (instrument, options) => { if (instrument.Meter == m) { publishCounter = instrument; } }
            };

            Assert.Null(publishCounter);
            listener.Start();
            Assert.Null(publishCounter);
            listener.Dispose();
            Assert.Null(publishCounter);
            Counter c = m.CreateCounter("C");
            Assert.Null(publishCounter);
        }

        [Fact]
        public void UnpublishListenerDispose()
        {
            using Meter m = new Meter("TestMeterH");
            Counter c = m.CreateCounter("C");
            MeterInstrument unpublishCounter = null;
            using MeterInstrumentListener listener = new MeterInstrumentListener()
            {
                MeterInstrumentPublished = (instrument, options) => { if (instrument.Meter == m) { options.Subscribe(); } },
                MeterInstrumentUnpublished = (instrument, cookie) => unpublishCounter = instrument
            };

            listener.Start();
            Assert.Null(unpublishCounter);
            listener.Dispose();
            Assert.Equal(c, unpublishCounter);
        }

        [Fact]
        public void NoUnpublishListenerDoubleDispose()
        {
            using Meter m = new Meter("TestMeterI");
            Counter c = m.CreateCounter("C");
            MeterInstrument unpublishCounter = null;
            using MeterInstrumentListener listener = new MeterInstrumentListener()
            {
                MeterInstrumentPublished = (instrument, options) => { if (instrument.Meter == m) { options.Subscribe(); } },
                MeterInstrumentUnpublished = (instrument, cookie) => unpublishCounter = instrument
            };

            listener.Start();
            Assert.Null(unpublishCounter);
            listener.Dispose();
            Assert.Equal(c, unpublishCounter);
            unpublishCounter = null;
            listener.Dispose();
            Assert.Null(unpublishCounter);
        }
    }
}
