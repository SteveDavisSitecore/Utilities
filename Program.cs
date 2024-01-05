using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using OrderCloud.SDK;
using Utilities.Helpers;
using static System.Net.WebRequestMethods;

namespace Utilities
{
    public class Utilities
    {
        private static IOrderCloudClient _ocIntegrationClient;
        public static AppSettings _settings;

        public static async Task Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false);

            IConfiguration config = builder.Build();
            _settings = config.GetSection("AppSettings").Get<AppSettings>();

            _ocIntegrationClient = new OrderCloudClient(new OrderCloudClientConfig
            {
                ClientId = _settings.IntegrationClientId.ToSafeID(),
                ClientSecret = _settings.IntegrationClientSecret,
                GrantType = GrantType.ClientCredentials,
                ApiUrl = _settings.ApiUrl,
                AuthUrl = _settings.AuthUrl,
                Roles = new[]
                {
                    ApiRole.FullAccess
                }
            });

            //Console.WriteLine("Next step is to create price schedules. Type Y to continue, N to skip");
            //var create_price_schedules = Console.ReadLine();
            //if (create_price_schedules.ToLower() == "y")
            //{
            //    await CreatePriceSchedules();
            //}

            Console.WriteLine("Next step is to create products, price schedules, and assignments. Type Y to continue, N to skip");
            var create_products = Console.ReadLine();
            if (create_products.ToLower() == "y")
            {
                await CreateProducts();
            }

            Console.WriteLine("Next step is to create suppliers. Type Y to continue, N to skip");
            var create_suppliers = Console.ReadLine();
            if (create_suppliers.ToLower() == "y")
            {
                await CreateSuppliers();
            }
        }
        //private static async Task CreatePriceSchedules()
        //{
        //    Console.WriteLine($"Beginning price schedule create: {DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}");
        //    var tracker = new Tracker();
        //    tracker.Every(1.Minutes(), Methods.LogProgress);
        //    tracker.OnComplete(Methods.LogProgress);
        //    tracker.Start();

        //    var priceSchedules = new CreateResourcePipeline(_ocIntegrationClient, _settings);
        //    await priceSchedules.RunCreatePriceSchedulesAsync(_ocIntegrationClient, tracker);

        //    tracker.Stop();
        //    tracker.Now(Methods.LogProgress);
        //    await tracker.CompleteAsync();
        //    Console.WriteLine($"price schedule create complete: {DateTime.Now.ToShortDateString()} {DateTime.Now.ToLongTimeString()}");
        //}

        private static async Task CreateProducts()
        {
            Console.WriteLine($"Beginning product create: {DateTime.Now.ToShortDateString()} {DateTime.Now.ToLongTimeString()}");
            var tracker = new Tracker();
            tracker.Every(1.Minutes(), Methods.LogProgress);
            tracker.OnComplete(Methods.LogProgress);
            tracker.Start();

            var products = new CreateResourcePipeline(_ocIntegrationClient, _settings);
            await products.RunProductSetupAsync(_ocIntegrationClient, tracker);

            tracker.Stop();
            tracker.Now(Methods.LogProgress);
            await tracker.CompleteAsync();
            Console.WriteLine($"Product create complete: {DateTime.Now.ToShortDateString()} {DateTime.Now.ToLongTimeString()}");
        }

        private static async Task CreateSuppliers()
        {
            Console.WriteLine($"Beginning supplier create: {DateTime.Now.ToShortDateString()} {DateTime.Now.ToLongTimeString()}");
            var tracker = new Tracker();
            tracker.Every(1.Minutes(), Methods.LogProgress);
            tracker.OnComplete(Methods.LogProgress);
            tracker.Start();

            var products = new CreateResourcePipeline(_ocIntegrationClient, _settings);
            await products.RunCreateSuppliersAsync(_ocIntegrationClient, tracker);

            tracker.Stop();
            tracker.Now(Methods.LogProgress);
            await tracker.CompleteAsync();
            Console.WriteLine($"Supplier create complete: {DateTime.Now.ToShortDateString()} {DateTime.Now.ToLongTimeString()}");
        }
    }
}
