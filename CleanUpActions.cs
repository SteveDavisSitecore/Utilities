using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OrderCloud.SDK;
using Utilities.Helpers;

namespace Utilities
{

    public class CleanUpActions
    {
        private readonly IOrderCloudClient _oc;
        public CleanUpActions(IOrderCloudClient oc)
        {
            _oc = oc;
        }

        public async Task DeleteAllUnsubmittedOrders()
        {
            var orders = await _oc.Orders.ListAsync(OrderDirection.Incoming, filters: "Status=Unsubmitted", pageSize: 100);
            await Throttler.RunAsync(orders.Items, 300, 10,
                order => _oc.Orders.DeleteAsync(OrderDirection.Incoming, order.ID));
        }

        public async Task DeleteAllProducts()
        {
            var products = await _oc.Products.ListAsync(pageSize: 100);
            await Throttler.RunAsync(products.Items, 300, 10, product => _oc.Products.DeleteAsync(product.ID));

            var ps = await _oc.PriceSchedules.ListAsync(pageSize: 100);
            await Throttler.RunAsync(ps.Items, 300, 10, schedule => _oc.PriceSchedules.DeleteAsync(schedule.ID));
        }
    }
}
