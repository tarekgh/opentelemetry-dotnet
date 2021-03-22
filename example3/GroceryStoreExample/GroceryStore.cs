using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.Metric;

namespace GroceryStoreExample
{
    public class GroceryStore
    {
        private static Dictionary<string,double> price_list = new()
        {
            { "potato", 1.10 },
            { "tomato", 3.00 },
        };

        private string store_name;

        private Counter item_counter;
        private Counter cash_counter;

        public GroceryStore(string store_name)
        {
            this.store_name = store_name;

            // TODO: Is GroceryStore a singleton? This example is only good guidance if it is.
            // Otherwise we'd probably want MetricSource and the counters to be statics and
            // Store should be a dimension on the counters rather than a static label.

            Meter meter = new Meter("GroceryStore", "1.0.0",
                new Dictionary<string, string>()
                {
                    { "Store", store_name }
                });


            item_counter = new Counter("GroceryStore.item_counter",
                meter);
            cash_counter = new Counter("GroceryStore.cash_counter",
                meter);
        }

        public void process_order(string customer, params (string name, int qty)[] items)
        {
            double total_price = 0;

            foreach (var item in items)
            {
                total_price += item.qty * price_list[item.name];

                // Record Metric
                item_counter.Add(item.qty, ("Item", item.name), ("Customer", customer));
            }

            // Record Metric
            cash_counter.Add(total_price, ("Customer", customer));
        }
    }
}
