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
        private const int TEST_BATCH = 100;
        private const int SKIP_COUNT = 1000000;
        private const int PRODUCT_COUNT = 50000;
        private const int SUPPLIER_START_INDEX = 0;
        private const int SUPPLIER_COUNT = 700;
        private const int MIN_PAUSE = 10;
        private const int MAX_CONCURRENT = 1000;
        private const string BUYER_ID = "BuyerInteropID";
        private const string USERGROUP_ID = "AnonymousUserGroupInteropID";

        public CreateResourcePipeline(IOrderCloudClient oc, AppSettings settings)
        {
            _oc = oc;
            _settings = settings;
        }

        //public void Run(List<string> files, Tracker tracker)
        //{
        //    foreach (var file in files)
        //    {
        //        var products = ProductMapping(file, tracker);
        //        Methods.WritePriceSchedule(_settings.CatalogID); // using catalog ID for convenience
        //        Methods.WriteCatalog(_settings.CatalogID);
        //        Methods.WriteCategory(Categories(products));
        //        Methods.WriteFacet(Facets(products));
        //        Methods.WriteProducts(products, file);
        //    }
        //}

        //public async Task FixFacets(IOrderCloudClient oc)
        //{
        //    for(var i = 1; i <= 25; i++)
        //    {
        //        var list = await oc.ProductFacets.ListAsync(page: i, pageSize: 100);
        //        await Throttler.RunAsync(list.Items, 10, 100, async facet =>
        //        {
        //            await oc.ProductFacets.PatchAsync(facet.ID, new PartialProductFacet()
        //            {
        //                XpPath = $"facets.{facet.ID}",
        //                MinCount = 100
        //            });
        //            Console.WriteLine($"{facet.ID}");
        //        });
        //    }
        //}

        //public async Task DeleteFacets(IOrderCloudClient oc)
        //{
        //    for (var i = 1; i <= 25; i++)
        //    {
        //        var list = await oc.ProductFacets.ListAsync(page: i, pageSize: 100);
        //        await Throttler.RunAsync(list.Items, 10, 100, async facet =>
        //        {
        //            await oc.ProductFacets.DeleteAsync(facet.ID);
        //        });
        //    }
        //}

        //public async Task RunAsync(IOrderCloudClient oc, List<string> files, Tracker tracker)
        //{
        //    Console.WriteLine("starting");
        //    //await Methods.PutPriceSchedule(oc);

        //    foreach (var file in files)
        //    {
        //        //Console.WriteLine($"Staring file {file}");
        //        //var products = ProductMapping(file, tracker);
        //        var ids = ProductIDs(file);

        //        var products = new Dictionary<string, decimal>();
        //        foreach(var p in ids)
        //        {
        //            products.Add(p, new Random().Next(10, 100));   
        //        }
        //        using (var stream = File.CreateText("C:\\Repositories\\Utilities\\Log\\ids.txt"))
        //            foreach(var s in products)
        //            {
        //                stream.WriteLine(s);
        //            }

        //        tracker.ItemsDiscovered(products.Count);

        //        //Console.WriteLine($"PUT categories");
        //        //await Throttler.RunAsync(Categories(products), 10, 1000, async f =>
        //        //{
        //        //    await Methods.PutCategory(oc, f, _settings.CatalogID);
        //        //});
        //        //Console.WriteLine($"PUT facets");
        //        //await Throttler.RunAsync(Facets(products), 10, 1000, async f =>
        //        //{
        //        //    await Methods.PutFacet(oc, f);
        //        //});
        //        Console.WriteLine("PUT products");
        //        await Throttler.RunAsync(products, 20, 1000, async p =>
        //        {
        //            //await Methods.PutProducts(oc, p, _settings.CatalogID, tracker);
        //            //await Methods.PatchProducts(oc, p, _settings.CatalogID, tracker);
        //            //await Methods.PutPriceSchedules(oc, p, tracker);
        //        });
        //        await Throttler.RunAsync(products, 20, 1000, async p =>
        //        {
        //            await Methods.PatchProducts(oc, p, tracker);
        //        });
        //        Console.WriteLine("Complete");
        //    }
        //}

        public class InventoryRecordItem
        {
            public string ProductID { get; set; }
            public InventoryRecord Record { get; set; }
        }
        public class PriceScheduleItem
        {
            public string ProductID { get; set; }
            public OrderCloud.SDK.PriceSchedule PriceSchedule { get; set; }
        }
        public class DefaultSupplierPricingItem
        {
            public string ProductID { get; set; }
            public string SupplierID { get; set; }
            public string PriceScheduleID { get; set; }
        }

        public async Task RunCreateProductsAsync(IOrderCloudClient oc, Tracker tracker)
        {
            Console.WriteLine("Mapping Products");

            var products = new Dictionary<decimal, Product<PSPXp>>();

            for (var i = 0; i < PRODUCT_COUNT; i++)
            {
                products.Add(i, MapProduct(i));
            }

            tracker.ItemsDiscovered(products.Count());

            Console.WriteLine("PUT Products");
            await Throttler.RunAsync(products, MIN_PAUSE, MAX_CONCURRENT, async p =>
            {
                await Methods.PutProducts(oc, p.Value, tracker);
            });
            Console.WriteLine("PUT Products Complete");
        }

        public async Task RunCreateInventoryRecordsAsync(IOrderCloudClient oc, Tracker tracker)
        {
            Console.WriteLine("PUT Inventory Records");

            for (var s = SUPPLIER_START_INDEX; s < SUPPLIER_COUNT; s++) // start index in case this needs to be restarted
            {
                Console.WriteLine($"Starting Inventory Records for SupplierID_{s+1}");

                var inventoryRecords = new Dictionary<decimal, InventoryRecordItem>();

                for (var i = 0;
                     i < PRODUCT_COUNT;
                     i++) 
                {
                    var productID = string.Concat("ProductID_", i + 1);
                    var record = MapInventoryRecord(productID, s + 1);
                    inventoryRecords.Add(i, new InventoryRecordItem(){ProductID = productID, Record = record} );
                }
                tracker.ItemsDiscovered(inventoryRecords.Count());

                await Throttler.RunAsync(inventoryRecords, MIN_PAUSE, MAX_CONCURRENT, async p =>
                {
                    await Methods.PutInventoryRecords(oc, p.Value.Record, p.Value.ProductID, tracker);
                });
            }

            Console.WriteLine("PUT Inventory Records Complete");
        }

        public async Task RunCreatePriceSchedulesAsync(IOrderCloudClient oc, Tracker tracker)
        {
            Console.WriteLine("PUT Price Schedules");

            for (var s = SUPPLIER_START_INDEX; s < SUPPLIER_COUNT; s++) // start index in case this needs to be restarted
            {
                Console.WriteLine($"Starting Price Schedules for SupplierID_{s + 1}");

                var priceSchedules = new Dictionary<decimal, PriceScheduleItem>();

                for (var i = 0;
                     i < PRODUCT_COUNT;
                     i++)
                {
                    var productID = string.Concat("ProductID_", i + 1);
                    var anonPs = MapPriceSchedule(productID, s + 1, true);
                    var profiledPs = MapPriceSchedule(productID, s + 1, false);
                    priceSchedules.Add(i, new PriceScheduleItem() { ProductID = productID, PriceSchedule = anonPs });
                    priceSchedules.Add(i, new PriceScheduleItem() { ProductID = productID, PriceSchedule = profiledPs });
                }
                tracker.ItemsDiscovered(priceSchedules.Count());

                await Throttler.RunAsync(priceSchedules, MIN_PAUSE, MAX_CONCURRENT, async p =>
                {
                    await Methods.PutPriceSchedules(oc, p.Value.PriceSchedule, p.Value.ProductID, tracker);
                });
            }

            Console.WriteLine("PUT Price Schedules Complete");
        }

        public async Task RunProductAssignmentAsync(IOrderCloudClient oc, Tracker tracker)
        {
            Console.WriteLine("Product Assignments");

            for (var s = SUPPLIER_START_INDEX; s < SUPPLIER_COUNT; s++) // start index in case this needs to be restarted
            {
                Console.WriteLine($"Starting Product Assignments for SupplierID_{s + 1}");

                var productAssignments = new Dictionary<decimal, ProductAssignment>();

                for (var i = 0;
                     i < PRODUCT_COUNT;
                     i++)
                {
                    var productID = string.Concat("ProductID_", i + 1);
                    var productAssignment = MapProductAssignment(productID, s + 1);
                    productAssignments.Add(i, productAssignment);
                }
                tracker.ItemsDiscovered(productAssignments.Count());

                await Throttler.RunAsync(productAssignments, MIN_PAUSE, MAX_CONCURRENT, async p =>
                {
                    await Methods.AssignProductsAsync(oc, p.Value, tracker);
                });
            }

            Console.WriteLine("Product Assignments Complete");
        }

        public async Task RunDefaultSupplierPricingAsync(IOrderCloudClient oc, Tracker tracker)
        {
            Console.WriteLine("Default supplier pricing");

            for (var s = SUPPLIER_START_INDEX; s < SUPPLIER_COUNT; s++) // start index in case this needs to be restarted
            {
                Console.WriteLine($"Starting default supplier pricing for SupplierID_{s + 1}");

                var productAssignments = new Dictionary<decimal, DefaultSupplierPricingItem>();

                for (var i = 0;
                     i < PRODUCT_COUNT;
                     i++)
                {
                    var supplierID = string.Concat("SupplierID_", s + 1);
                    var productID = string.Concat("ProductID_", i + 1);
                    var psID = productID + "_PriceSchedule_" + (s + 1);
                    productAssignments.Add(i, new DefaultSupplierPricingItem(){PriceScheduleID = psID, ProductID = productID, SupplierID = supplierID });
                }
                tracker.ItemsDiscovered(productAssignments.Count());

                await Throttler.RunAsync(productAssignments, MIN_PAUSE, MAX_CONCURRENT, async p =>
                {
                    await Methods.AssignDefaultPricingAsync(oc, p.Value.ProductID, p.Value.SupplierID, p.Value.PriceScheduleID, tracker);
                });
            }

            Console.WriteLine("Default supplier pricing Complete");
        }

        //private List<OrderCloud.SDK.Category> Categories(HashSet<Product<BokusXp>> hash)
        //{
        //    //var grouped = from item in hash
        //    //              from cat in item.xp.categories
        //    //              select cat;
        //    //return grouped.DistinctBy(c => new { c.code, c.name }).Select(c => new OrderCloud.SDK.Category()
        //    //{
        //    //    Active = true,
        //    //    Description = c.name,
        //    //    ID = c.code.ToSafeID(),
        //    //    Name = c.name
        //    //}).ToList();
        //    throw new NotImplementedException();
        //}

        //private List<ProductFacet> Facets(HashSet<Product<BokusXp>> hash)
        //{
        //    throw new NotImplementedException();
        //    //var list = new List<ProductFacet>();
        //    //var grouped = hash.GroupBy(p => p.xp.facets, (key, group) => group.First().xp.facets).ToList();
        //    //list.AddRange(from item in grouped
        //    //              from KeyValuePair<string, object> facet in item
        //    //              let f = new ProductFacet()
        //    //              {
        //    //                  ID = facet.Key.ToSafeID(),
        //    //                  Name = facet.Value.ToString(),
        //    //                  XpPath = facet.Key
        //    //              }
        //    //              select f);
        //    //var unique = list.DistinctBy(f => new { f.ID, f.Name }).Select(f => new ProductFacet()
        //    //{
        //    //    ID = f.ID,
        //    //    Name = f.Name ?? "Other",
        //    //    XpPath = f.ID
        //    //}).ToList();
        //    //return unique;
        //}

        //private List<string> ProductIDs(string file)
        //{
        //    return new HashSet<string>(File.ReadLines(file))
        //        .Skip(SKIP_COUNT)
        //        .Take(TEST_BATCH)
        //        .ToList();
        //}

        public static Product<PSPXp> MapProduct(int index)
        {
            var productID = string.Concat("ProductID_", index + 1);

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
            product.AllSuppliersCanSell = true;
            product.Active = true;
            product.Description = (string)string.Concat("Description_", productID);
            product.Name = (string)string.Concat("Name_", productID);

            product.xp.Color = (string)productColors[productColorsIndex];
            product.xp.Size = (string)productSizes[productSizesIndex];
            product.xp.Brand = (string)productBrands[productBrandsIndex];

            return product;
        }

        public static InventoryRecord MapInventoryRecord(string productID, int supplierIndex)
        {
            var inventoryRecordID = string.Concat(productID, "_InventoryRecordID_", supplierIndex);

            return new InventoryRecord()
            {
                ID = inventoryRecordID,
                AddressID = string.Concat("SupplierAddressID_", supplierIndex),
                QuantityAvailable = 100,
                OwnerID = string.Concat("SupplierID_", supplierIndex),
                AllowAllBuyers = true,
                OrderCanExceed = true
            };
        }

        public static OrderCloud.SDK.PriceSchedule MapPriceSchedule(string productID, int supplierIndex, bool isAnon)
        {
            var psID = productID + "_PriceSchedule_" + supplierIndex + (isAnon ? "_Anon" : "");

            return new OrderCloud.SDK.PriceSchedule()
            {
                ID = psID,
                OwnerID = string.Concat("SupplierID_", supplierIndex),
                Name = string.Concat("Name_", psID),
                PriceBreaks = new[] { new PriceBreak { Price = isAnon ? 10 : 5, Quantity = 1 } }, 
                MinQuantity = 1,
                ApplyShipping = false,
                ApplyTax = false
            };
        }

        public static ProductAssignment MapProductAssignment(string productID, int supplierIndex)
        {
            var psID = productID + "_PriceSchedule_" + supplierIndex + "_Anon";

            return new ProductAssignment()
            {
                ProductID = productID,
                PriceScheduleID = psID,
                BuyerID = BUYER_ID,
                UserGroupID = USERGROUP_ID,
                SellerID = string.Concat("SupplierID_", supplierIndex)
            };
        }

        //private HashSet<Product<BokusXp>> ProductMapping(string file, Tracker tracker)
        //{
        //    var list = new HashSet<Product<BokusXp>>();
        //    using (System.IO.Stream fs = new FileStream(file, FileMode.Open, FileAccess.Read))
        //    using (StreamReader streamReader = new StreamReader(fs))
        //    using (JsonTextReader reader = new JsonTextReader(streamReader))
        //    {
        //        reader.SupportMultipleContent = true;
        //        var serializer = new JsonSerializer();
        //        serializer.Converters.Add(new BokusConverter());
               
        //        while (reader.Read())
        //        {
        //            if (reader.TokenType == JsonToken.StartObject)
        //            {
        //                var t = serializer.Deserialize<Product<BokusXp>>(reader);
        //                list.Add(t);
        //            }
        //        }
        //    }
        //    return list;

        //    // worked great with smaller files
        //    //using StreamReader r = new StreamReader(file);
        //    //string json = r.ReadToEnd();

        //    //HashSet<Product<BokusXp>> list = JsonConvert.DeserializeObject<HashSet<Product<BokusXp>>>(json, new JsonSerializerSettings()
        //    //{
        //    //    Culture = CultureInfo.InvariantCulture,
        //    //    Converters = new List<JsonConverter> { new BokusConverter() }
        //    //});
        //    //return list;
        //}
    }
}
