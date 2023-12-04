//using System;
//using System.Collections.Generic;
//using System.Text;

//namespace Utilities
//{
//    using global::Utilities.Helpers;
//    using OrderCloud.SDK;
//    using System.Diagnostics;
//    using System.Linq;
//    using System.Threading.Tasks;

//    namespace CatalogUploader
//    {
//        internal class PriceUpdater
//        {
//            private readonly List<Product> uniqueProductList = [];
//            private readonly OrderCloudClient orderCloudClient;

//            private const int MAX_PRODUCTS = 1;

//            private const int DEFAULT_PRICE = 30;

//            public PriceUpdater()
//            {

//            }

//            public async Task RunAsync()
//            {
//                // Get products with unique prices
//                await GetProductsRecursive(1, new Dictionary<string, object>() { { "xp.uniquePrice", true } });

//                if (uniqueProductList.Count == 0)
//                {
//                    Console.WriteLine("No unique priced products found. Assign products.");

//                    // Get products without unique prices
//                    await GetProductsRecursive(1, new Dictionary<string, object>() { { "xp.uniquePrice", "!*" } });

//                    // Seed the products for unique prices
//                    await SeedProducts();
//                }

//                // We assume all products has default prices.

//                Console.WriteLine($"Begin updating prices on {uniqueProductList.Count} products.");

//                if (uniqueProductList.Count > 0)
//                {
//                    var timer = Stopwatch.StartNew();

//                    await UpdatePrices();

//                    timer.Stop();

//                    var sampleProds = string.Join(",", uniqueProductList.Take(5).Select(x => x.ID));

//                    Console.WriteLine($"Updated {uniqueProductList.Count} products in {timer.Elapsed}");
//                    Console.WriteLine($"Sample IDs: {sampleProds}");

//                    var sampleProduct = await orderCloudClient.Products.GetAsync(uniqueProductList.First().ID);

//                    Console.ForegroundColor = ConsoleColor.Cyan;
//                    Console.WriteLine($"[{sampleProduct.ID}].DefaultPriceScheduleID = {sampleProduct.DefaultPriceScheduleID}");

//                    var xpDictionary = (IDictionary<string, object?>?)sampleProduct.xp!;

//                    if (xpDictionary!.ContainsKey("uniquePrice"))
//                    {
//                        Console.WriteLine($"[{sampleProduct.ID}].xp.uniquePrice = {sampleProduct.xp.uniquePrice}");
//                    }

//                    if (xpDictionary.ContainsKey("sortPrice"))
//                    {
//                        Console.WriteLine($"[{sampleProduct.ID}].xp.sortPrice = {sampleProduct.xp.sortPrice}");
//                    }
//                }

//            }

//            private async Task SeedProducts()
//            {
//                await Throttler.RunAsync(uniqueProductList, 40, 300, async product =>
//                {
//                    try
//                    {
//                        var patchedProduct = await orderCloudClient.Products.PatchAsync(product.ID, new PartialProduct
//                        {
//                            ID = product.ID,
//                            xp = new { uniquePrice = true }
//                        });

//                        var priceId = GetDefaultPriceId(product);
//                        await orderCloudClient.PriceSchedules.SaveAsync(priceId, new PriceSchedule
//                        {
//                            ID = priceId,
//                            Currency = "USD",
//                            Name = $"{product.ID} default price",
//                            MinQuantity = 1,
//                            MaxQuantity = 100,
//                            PriceBreaks = new[] { new PriceBreak { Price = DEFAULT_PRICE, Quantity = 1 } }
//                        });
//                    }
//                    catch (Exception ex)
//                    {
//                        Console.WriteLine(ex.Message);
//                    }
//                });
//            }

//            private async Task UpdatePrices()
//            {
//                await Throttler.RunAsync(uniqueProductList, 20, 1000, async product =>
//                {
//                    try
//                    {
//                        var patchedProduct = await orderCloudClient.Products.PatchAsync(product.ID, new PartialProduct
//                        {
//                            ID = product.ID,
//                            DefaultPriceScheduleID = GetDefaultPriceId(product),
//                            xp = new
//                            {
//                                sortPrice = DEFAULT_PRICE
//                            }
//                        });

//                        await orderCloudClient.PriceSchedules.PatchAsync(GetDefaultPriceId(product),
//                                                                         new PartialPriceSchedule
//                                                                         {
//                                                                             ID = GetDefaultPriceId(product),
//                                                                             PriceBreaks = new[] { new PriceBreak
//                                                                         {
//                                                                             Price = DEFAULT_PRICE,
//                                                                             Quantity = 1
//                                                                         } }
//                                                                         });
//                    }
//                    catch (Exception ex)
//                    {
//                        Console.WriteLine(ex.Message);
//                    }
//                });
//            }

//            private string GetDefaultPriceId(Product product) => $"{product.ID}_USD";

//            private async Task GetProductsRecursive(int page, object filters)
//            {
//                var uniqueProducts = await orderCloudClient.Products.ListAsync(page: page,
//                                                                               pageSize: MAX_PRODUCTS > 100 ? 100 : MAX_PRODUCTS,
//                                                                               filters: filters);

//                uniqueProductList.AddRange(uniqueProducts.Items);

//                if (uniqueProducts.Meta.TotalPages > uniqueProducts.Meta.Page && uniqueProductList.Count < MAX_PRODUCTS)
//                {
//                    await GetProductsRecursive(page + 1, filters);
//                }
//            }

//        }
//    }
//}
