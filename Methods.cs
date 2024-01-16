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

        internal static async Task PutProducts(IOrderCloudClient oc, Product product, string catalogId, string categoryId, Tracker tracker)
        {
            try
            {
                await oc.PriceSchedules.SaveAsync(product.ID, new PriceSchedule()
                {
                    ID = product.ID,
                    Name = string.Concat("Name_", product.ID),
                    PriceBreaks = new[]
                        { new PriceBreak { Price = Convert.ToDecimal(new Random().Next(10, 100)), Quantity = 1 } },
                    MinQuantity = 1,
                    ApplyShipping = false,
                    ApplyTax = false
                });
                await oc.Products.SaveAsync(product.ID, product);
                await oc.Catalogs.SaveProductAssignmentAsync(new ProductCatalogAssignment()
                {
                    CatalogID = catalogId,
                    ProductID = product.ID
                });
                await oc.Categories.SaveProductAssignmentAsync(catalogId, new CategoryProductAssignment()
                {
                    CategoryID = categoryId,
                    ProductID = product.ID
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

        internal static async Task<ListPageWithFacets<Product>> ListProductsWithLastIDFilter(IOrderCloudClient oc, string productID)
        {
            return await oc.Products.ListAsync(filters: new { ID = productID });
        }

        internal static async Task<int> RetrieveMetaTotalCount(IOrderCloudClient oc)
        {
            var result = await oc.Products.ListAsync();
            return result.Meta.TotalCount;
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
