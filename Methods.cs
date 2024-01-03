using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OrderCloud.SDK;
using Utilities.Helpers;

namespace Utilities
{
    public static class Methods
    {
        public static void LogProgress(Progress progress)
        {
            Console.WriteLine($"{progress.ElapsedTime:hh\\:mm\\:ss} elapsed. {progress.ItemsDone} of {progress.TotalItems} complete ({progress.PercentDone}%)");
        }

        private static string CreateDir(string Name)
        {
            var dir = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);
            var createDir = Directory.CreateDirectory(dir + "\\Output\\" + Name);
            return createDir.FullName;
        }

        public static void WriteCategory(List<OrderCloud.SDK.Category> categories)
        {
                using StreamWriter file = File.CreateText($"{CreateDir("Categories")}\\catgegories.json");
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, categories);
        }

        public static async Task PutCategory(IOrderCloudClient oc, OrderCloud.SDK.Category category, string catalogId)
        {
            await oc.Categories.SaveAsync(catalogId, category.ID, category);
        }

        public static void WriteCatalog(string catalog)
        {
            using StreamWriter file = File.CreateText($"{CreateDir("Catalogs")}\\{catalog}.json");
            JsonSerializer serializer = new JsonSerializer();
            serializer.Serialize(file, new Catalog()
            {
                ID = catalog.ToSafeID(),
                Name = catalog,
                Active = true,
                Description = catalog
            });
        }

        public static async Task PutCatalog(IOrderCloudClient oc, string catalog, string buyerID)
        {
            await oc.Catalogs.SaveAsync(catalog, new Catalog()
            {
                ID = catalog.ToSafeID(),
                Name = catalog,
                Active = true,
                Description = catalog
            });
            await oc.Catalogs.SaveAssignmentAsync(new CatalogAssignment()
            {
                BuyerID = buyerID,
                CatalogID = catalog.ToSafeID(),
                ViewAllCategories = true,
                ViewAllProducts = true
            });
        }

        public static void WriteFacet(List<ProductFacet> facets)
        {
                using StreamWriter file = File.CreateText(CreateDir("Facets") + "\\facets.json");
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, facets);
        }

        public static async Task PutFacet(IOrderCloudClient oc, ProductFacet facet)
        {
            try
            {
                await oc.ProductFacets.SaveAsync(facet.ID, facet);
            }
            catch (Exception)
            {
                Console.WriteLine($"Facet error: {facet.ID}");
            }
        }

        internal static void WritePriceSchedule(string priceScheduleID)
        {
            using StreamWriter file = File.CreateText($"{CreateDir("PriceSchedules")}\\{priceScheduleID}.json");
            JsonSerializer serializer = new JsonSerializer();
            serializer.Serialize(file, new PriceSchedule()
            {
                ID = priceScheduleID.ToSafeID(),
                Name = priceScheduleID,
                PriceBreaks = new List<PriceBreak>()
                {
                    new PriceBreak()
                    {
                        Price = 15,
                        Quantity = 1
                    }
                }
            });
        }
        internal static async Task PutPriceSchedule(IOrderCloudClient oc)
        {
            await oc.PriceSchedules.SaveAsync("DefaultPrice", new PriceSchedule()
            {
                ID = "DefaultPrice",
                Name = "Default Price",
                PriceBreaks = new List<PriceBreak>()
            {
                new PriceBreak()
                {
                    Price = 15,
                    Quantity = 1
                }
            }
            });
        }

        internal static void WriteProducts(HashSet<Product<BokusXp>> products, string fileName)
        {
            var set = 10000;
            for (int i = 0; i < products.Count(); i += set)
            {
                var subset = products.Skip(i).Take(set);
                
                using StreamWriter file = File.CreateText($"{CreateDir("Products")}\\{fileName.Replace(".json", "")}_{i}-{i + (set -1)}.json");
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, subset);
            }
        }

        internal static async Task PatchProducts(IOrderCloudClient oc, Product<BokusXp> product, string catalogId, Tracker tracker)
        {
            try
            {
                var attempt = await oc.Products.PatchAsync(product.ID, new PartialProduct()
                {
                    xp = product.xp
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                tracker.ItemFailed();
            }
            finally
            {
                tracker.ItemSucceeded();
            }
        }

        internal static async Task<string> PutProducts(IOrderCloudClient oc, Product product, Tracker tracker)
        {
            try
            {
                await oc.Products.SaveAsync(product.ID, product);
                return product.ID;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                tracker.ItemFailed();
                return null;
            }
            finally
            {
                tracker.ItemSucceeded();
            }
        }


        internal static async Task<ListPageWithFacets<Product>> ListProductsWithLastIDFilter(IOrderCloudClient oc, string productID)
        {
            return await oc.Products.ListAsync(filters: new { ID = productID });
        }
    }

    public static class Extensions
    {
        public static string ToSafeID(this string ID)
        {
            var regex = new Regex("[^A-Za-z0-9-_.]");
            return regex.Replace(ID, "_");
        }

        public static string TruncateLongString(this string str, int maxLength) =>
            str?[0..Math.Min(str.Length, maxLength)];

        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            var known = new HashSet<TKey>();
            return source.Where(element => known.Add(keySelector(element)));
        }
    }
}
