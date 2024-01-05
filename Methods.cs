using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

        internal static async Task PutProducts(IOrderCloudClient oc, Product<PSPXp> product, Tracker tracker)
        {
            try
            {
                await oc.Products.SaveAsync(product.ID, product);
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

        internal static async Task PutPriceSchedules(IOrderCloudClient oc, PriceSchedule ps, Tracker tracker)
        {
            try
            {
                await oc.PriceSchedules.SaveAsync(ps.ID, ps);
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

        internal static async Task SaveCatalogAssignment(IOrderCloudClient oc, ProductCatalogAssignment assignment, Tracker tracker)
        {
            try
            {
                await oc.Catalogs.SaveProductAssignmentAsync(assignment);
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

        internal static async Task PutSuppliers(IOrderCloudClient oc, Supplier supplier, Tracker tracker)
        {
            try
            {
                await oc.Suppliers.SaveAsync(supplier.ID, supplier);
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
