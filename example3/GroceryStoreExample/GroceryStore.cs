using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.Metric;

namespace GroceryStoreExample
{
    public class GroceryStore
    {
        private static Dictionary<string,double> s_priceList = new()
        {
            { "potato", 1.10 },
            { "tomato", 3.00 },
        };

        static Meter s_meter = new Meter("GroceryStore", "1.0.0");
        static private Counter s_itemCounter = s_meter.CreateCounter("item_counter");
        static private Counter s_cashCounter = s_meter.CreateCounter("cash_counter");

        private string _storeName;

        public GroceryStore(string storeName)
        {
            this._storeName = storeName;
        }

        public void ProcessOrder(string customer, params (string name, int qty)[] items)
        {
            double totalPrice = 0;

            foreach (var item in items)
            {
                totalPrice += item.qty * s_priceList[item.name];

                // Record Metric
                s_itemCounter.Add(item.qty, ("StoreName", _storeName), ("Item", item.name), ("Customer", customer));
            }

            // Record Metric
            s_cashCounter.Add(totalPrice, ("StoreName", _storeName), ("Customer", customer));
        }
    }
}
