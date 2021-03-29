using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Metric
{
    internal class MeterInstrumentCollection
    {
        public static MeterInstrumentCollection Instance = new MeterInstrumentCollection();

        // Even if we had multiple exposed instances of this collection in the future
        // this lock also synchronizes access to per-metric subscription lists so it
        // needs to remain global (or metric subscription lists need to be changed)
        static internal object Lock = new object();

        List<MeterInstrumentBase> _instruments = new List<MeterInstrumentBase>();
        List<MeterInstrumentListener> _listeners = new List<MeterInstrumentListener>();
        MeterSubscribeOptions _subscribeOptions = new MeterSubscribeOptions();

        public void AddMetric(MeterInstrumentBase instrument)
        {
            lock(Lock)
            {
                _instruments.Add(instrument);
                foreach(MeterInstrumentListener listener in _listeners)
                {
                    NotifyListenerMetricAdd(listener, instrument);
                }
            }
        }

        public void RemoveMetric(MeterInstrumentBase instrument)
        {
            lock (Lock)
            {
                _instruments.Remove(instrument);
                foreach (MeterInstrumentListener listener in _listeners)
                {
                    NotifyListenerMetricRemove(listener, instrument);
                }
            }
        }

        public void AddListener(MeterInstrumentListener listener)
        {
            lock(Lock)
            {
                _listeners.Add(listener);
                foreach(MeterInstrumentBase instrument in _instruments)
                {
                    NotifyListenerMetricAdd(listener, instrument);
                }
            }
        }

        public void RemoveListener(MeterInstrumentListener listener)
        {
            lock (Lock)
            {
                _listeners.Remove(listener);
                foreach (MeterInstrumentBase instrument in _instruments)
                {
                    NotifyListenerMetricRemove(listener, instrument);
                }
            }
        }

        void NotifyListenerMetricAdd(MeterInstrumentListener listener, MeterInstrumentBase instrument)
        {
            _subscribeOptions.Reset();
            listener.MeterInstrumentPublished?.Invoke(instrument, _subscribeOptions);
            if (_subscribeOptions.IsSubscribed)
            {
                if (!instrument.IsObservable)
                {
                    instrument.AddSubscription(listener, _subscribeOptions.Cookie);
                }
                else
                {
                    listener.SubscribeObservableInstrument(instrument, _subscribeOptions.Cookie);
                }
            }
        }

        void NotifyListenerMetricRemove(MeterInstrumentListener listener, MeterInstrumentBase instrument)
        {
            object cookie = null;
            if (!instrument.IsObservable)
            {
                 cookie = instrument.RemoveSubscription(listener);
            }
            else
            {
                cookie = listener.UnsubscribeObservableInstrument(instrument);
            }
            listener.MeterInstrumentUnpublished?.Invoke(instrument, cookie);
        }
    }

    public class MeterSubscribeOptions
    {
        internal bool IsSubscribed { get; private set; }
        internal object Cookie { get; private set; }
        internal void Reset()
        {
            IsSubscribed = false;
            Cookie = null;
        }
        public void Subscribe()
        {
            IsSubscribed = true;
        }
        public void Subscribe(object cookie)
        {
            Cookie = cookie;
            IsSubscribed = true;
        }
    }
}
