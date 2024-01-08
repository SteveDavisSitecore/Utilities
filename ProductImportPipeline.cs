using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OrderCloud.SDK;
using Utilities.Helpers;
using static OrderCloud.SDK.ErrorCodes;
using static OrderCloud.SDK.WebhookPayloads;
using InventoryRecord = OrderCloud.SDK.InventoryRecord;

namespace Utilities
{
    public class CreateResourcePipeline
    {
        private readonly IOrderCloudClient _oc;
        private readonly AppSettings _settings;
        private const int PRODUCT_COUNT = 10;
        private const int SUPPLIER_COUNT = 10;
        private const int MIN_PAUSE = 20;
        private const int MAX_CONCURRENT = 1000;

        public CreateResourcePipeline(IOrderCloudClient oc, AppSettings settings)
        {
            _oc = oc;
            _settings = settings;
        }

        //public async Task RunCreatePriceSchedulesAsync(IOrderCloudClient oc, Tracker tracker)
        //{
        //    Console.WriteLine("PUT Price Schedules");

        //    var priceSchedules = new List<OrderCloud.SDK.PriceSchedule>();

        //    // unique ps for each product, use as default ps
        //    for (var i = 0; i < PRODUCT_COUNT; i++)
        //    {
        //        var ps = MapPriceSchedule(i);
        //        priceSchedules.Add(ps);
        //    }
        //    tracker.ItemsDiscovered(priceSchedules.Count());

        //    await Throttler.RunAsync(priceSchedules, MIN_PAUSE, MAX_CONCURRENT, async ps =>
        //    {
        //        await Methods.PutPriceSchedules(oc, ps);
        //    });

        //    Console.WriteLine("PUT Price Schedules Complete");
        //}

        public async Task RunProductSetupAsync(IOrderCloudClient oc, Tracker tracker)
        {

            var products = new Dictionary<string, Product<PSPXp>>();

            for (var i = 0; i < PRODUCT_COUNT; i++)
            {
                var productID = string.Concat("ProductID_", (i + 1).ToString("D5"));
                products.Add(productID, MapProduct(productID));
            }

            tracker.ItemsDiscovered(products.Count());

            Console.WriteLine("Creating Price Schedules");
            await Throttler.RunAsync(products, MIN_PAUSE, MAX_CONCURRENT, async p =>
            {
                await Methods.PutPriceSchedules(oc, MapPriceSchedule(p.Key), tracker);
            });
            Console.WriteLine("Creating Products");
            await Throttler.RunAsync(products, MIN_PAUSE, MAX_CONCURRENT, async p =>
            {
                await Methods.PutProducts(oc, p.Value, tracker);
            });
            Console.WriteLine("Creating Catalog Assignments");
            await Throttler.RunAsync(products, MIN_PAUSE, MAX_CONCURRENT, async p =>
            {
                await Methods.SaveCatalogAssignment(oc, MapCatalogAssignment(p.Key), tracker);
            });

            Console.WriteLine("Finished Product Setup");
            
        }

        public async Task RunCreateSuppliersAsync(IOrderCloudClient oc, Tracker tracker)
        {
            Console.WriteLine("Mapping Suppliers");

            var suppliers = new Dictionary<string, Supplier>();

            for (var i = 0; i < SUPPLIER_COUNT; i++)
            {
                var id = string.Concat("Supplier_", (i + 1).ToString("D3"));
                suppliers.Add(id, MapSupplier(id));
            }

            tracker.ItemsDiscovered(suppliers.Count());

            Console.WriteLine("PUT Suppliers");
            await Throttler.RunAsync(suppliers, MIN_PAUSE, MAX_CONCURRENT, async s =>
            {
                await Methods.PutSuppliers(oc, s.Value, tracker);
            });
            Console.WriteLine("PUT Supplier Addresses");
            await Throttler.RunAsync(suppliers, MIN_PAUSE, MAX_CONCURRENT, async s =>
            {
                var address = MapSupplierAddress(s.Key);
                await Methods.PutSupplierAddresses(oc, s.Key, address, tracker);
            });
            Console.WriteLine("PUT Suppliers Complete");
        }

        public static OrderCloud.SDK.PriceSchedule MapPriceSchedule(string productID)
        {
            var psID = string.Concat(productID, "_PriceSchedule");

            return new OrderCloud.SDK.PriceSchedule()
            {
                ID = psID,
                Name = string.Concat("Name_", psID),
                PriceBreaks = new[] { new PriceBreak { Price = Convert.ToDecimal(new Random().Next(10, 100)), Quantity = 1 } },
                MinQuantity = 1,
                ApplyShipping = false,
                ApplyTax = false
            };
        }

        public static Product<PSPXp> MapProduct(string productID)
        {
            var productColors = new string[] { "Red", "Green", "Blue" };
            var productSizes = new string[] { "S", "M", "L", "XL" };
            var productBrands = new string[] { "Alpha", "Beta", "Gamma", "Delta", "Zeta", "Kappa", "Lambda", "Mu", "Sigma", "Omega" };

            var productColorsIndex = new Random().Next(0, productColors.Length);
            var productSizesIndex = new Random().Next(0, productSizes.Length);
            var productBrandsIndex = new Random().Next(0, productBrands.Length);
            Product<PSPXp> product = new Product<PSPXp>()
            {
                xp = new PSPXp()
            };

            product.ID = (string)productID;
            product.AllSuppliersCanSell = true; // requirement for PSP setup
            product.DefaultPriceScheduleID = productID + "_PriceSchedule";
            product.Inventory = new OrderCloud.SDK.Inventory
            {
                Enabled = true,
                OrderCanExceed = true
            };
            product.Active = true;
            product.Description = (string)string.Concat("Description_", productID);
            product.Name = (string)string.Concat("Name_", productID);

            product.xp.Color = (string)productColors[productColorsIndex];
            product.xp.Size = (string)productSizes[productSizesIndex];
            product.xp.Brand = (string)productBrands[productBrandsIndex];

            return product;
        }

        public static Supplier MapSupplier(string id)
        {
            return new Supplier()
            {
                ID = id,
                Name = id,
                AllBuyersCanOrder = true, // requirement for PSP setup
                Active = true
            };
        }

        public static OrderCloud.SDK.Address MapSupplierAddress(string supplierID)
        {
            return new OrderCloud.SDK.Address()
            {
                ID = supplierID,
                AddressName = string.Concat("Name_", supplierID),
                Street1 = "Fake Street",
                City = "Fake City",
                State = "MN",
                Country = "US",
                Zip = "11111"
            };
        }

        private ProductCatalogAssignment MapCatalogAssignment(string productID)
        {

            return new ProductCatalogAssignment()
            {
                CatalogID = _settings.CatalogID,
                ProductID = productID
            };
        }
    }
}
