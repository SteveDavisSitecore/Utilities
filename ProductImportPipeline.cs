using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OrderCloud.SDK;
using Utilities.Helpers;

namespace Utilities
{
    public class ProductImportPipeline
    {
        private readonly IOrderCloudClient _oc;
        private readonly AppSettings _settings;

        public ProductImportPipeline(IOrderCloudClient oc, AppSettings settings)
        {
            _oc = oc;
            _settings = settings;
        }

        public async Task<string> RunAsync(IOrderCloudClient oc, Tracker tracker, int productCount, int pause, int max)
        {
            Console.WriteLine("Mapping Products");

            var products = new List<Product>();

            for (var i = 0; i < productCount; i++)
            {
                products.Add(MapMockProductData());
            }
            tracker.ItemsDiscovered(products.Count());

            Console.WriteLine($"Starting PUT products: {DateTime.Now.ToShortDateString()} {DateTime.Now.ToLongTimeString()}");
            await Throttler.RunAsync(products.Take(productCount - 1), pause, max, async p =>
            {
                await Methods.PutProducts(oc, p, _settings.CatalogID, _settings.CategoryID, tracker);
            });

            // ensure last product in list is last ID processed
            var lastProduct = products.Last();
            await Methods.PutProducts(oc, lastProduct, _settings.CatalogID, _settings.CategoryID, tracker);

            Console.WriteLine($"Last Product ID: {lastProduct.ID}");

            return lastProduct.ID;
        }

        public static Product MapMockProductData()
        {
            var productID = Guid.NewGuid().ToString();

            Product product = new Product();

            product.ID = (string)productID;
            product.Active = true;
            product.Description = (string)string.Concat("Description_", productID);
            product.Name = (string)string.Concat("Name_", productID);
            product.DefaultPriceScheduleID = productID;

            return product;
        }
    }
}
