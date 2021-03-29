using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
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

        List<Meter> _meters = new List<Meter>();
        List<MeterInstrumentListener> _listeners = new List<MeterInstrumentListener>();
        MeterSubscribeOptions _subscribeOptions = new MeterSubscribeOptions();


        public void AddMeter(Meter meter)
        {
            lock(Lock)
            {
                _meters.Add(meter);
                foreach (MeterInstrumentListener listener in _listeners)
                {
                    foreach (MeterInstrument instrument in meter.Instruments)
                    {
                        NotifyListenerInstrumentAdd(listener, instrument);
                    }
                }
            }
        }

        public void RemoveMeter(Meter meter)
        {
            lock (Lock)
            {
                _meters.Remove(meter);
                foreach (MeterInstrumentListener listener in _listeners)
                {
                    foreach (MeterInstrument instrument in meter.Instruments)
                    {
                        NotifyListenerInstrumentRemove(listener, instrument);
                    }
                }
            }
        }

        public void PublishInstrument(MeterInstrument instrument)
        {
            Debug.Assert(Monitor.IsEntered(Lock));
            foreach (MeterInstrumentListener listener in _listeners)
            {
                NotifyListenerInstrumentAdd(listener, instrument);
            }
        }

        public void AddListener(MeterInstrumentListener listener)
        {
            lock(Lock)
            {
                _listeners.Add(listener);
                foreach (Meter meter in _meters)
                {
                    foreach (MeterInstrument instrument in meter.Instruments)
                    {
                        NotifyListenerInstrumentAdd(listener, instrument);
                    }
                }
            }
        }

        public void RemoveListener(MeterInstrumentListener listener)
        {
            lock (Lock)
            {
                _listeners.Remove(listener);
                foreach (Meter meter in _meters)
                {
                    foreach (MeterInstrument instrument in meter.Instruments)
                    {
                        NotifyListenerInstrumentRemove(listener, instrument);
                    }
                }
            }
        }

        void NotifyListenerInstrumentAdd(MeterInstrumentListener listener, MeterInstrument instrument)
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

        void NotifyListenerInstrumentRemove(MeterInstrumentListener listener, MeterInstrument instrument)
        {
            bool wasSubscribed;
            object cookie = null;
            if (!instrument.IsObservable)
            {
                 wasSubscribed = instrument.RemoveSubscription(listener, out cookie);
            }
            else
            {
                wasSubscribed = listener.UnsubscribeObservableInstrument(instrument, out cookie);
            }
            if (wasSubscribed)
            {
                listener.MeterInstrumentUnpublished?.Invoke(instrument, cookie);
            }
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
