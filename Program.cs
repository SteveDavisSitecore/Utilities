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

            //Console.WriteLine("First step is to configure the buyer environment. Type Y to continue, N to skip");
            //var setup_buyer = Console.ReadLine();
            //if (setup_buyer.ToLower() == "y")
            //{
            //    await SetupBuyer();
            //}

            Console.WriteLine("Next step is to create products. Type Y to continue, N to skip");
            var create_products = Console.ReadLine();
            if (create_products.ToLower() == "y")
            {
                await CreateProducts();
            }

            Console.WriteLine("Next step is to create price schedules. Type Y to continue, N to skip");
            var create_price_schedules = Console.ReadLine();
            if (create_price_schedules.ToLower() == "y")
            {
                await CreatePriceSchedules();
            }

            Console.WriteLine("Next step is to create inventory records. Type Y to continue, N to skip");
            var create_records = Console.ReadLine();
            if (create_records.ToLower() == "y")
            {
                await CreateInventoryRecords();
            }

            Console.WriteLine("Next step is to create product assignments. Type Y to continue, N to skip");
            var product_assignments = Console.ReadLine();
            if (product_assignments.ToLower() == "y")
            {
                await AssignProducts();
            }

            Console.WriteLine("Next step is to set default pricing. Type Y to continue, N to skip");
            var default_pricing = Console.ReadLine();
            if (default_pricing.ToLower() == "y")
            {
                await SetDefaultPricing();
            }
        }

        private static async Task SetupBuyer()
        {
            var buyer = new ShopperImportPipeline(_ocIntegrationClient, _settings);
            await buyer.RunAsync();
        }

        //private static async Task ImportProducts()
        //{
        //    // just some on screen tracking information
        //    Console.WriteLine($"Beginning import: {DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}");
        //    var tracker = new Tracker();
        //    tracker.Every(1.Minutes(), Methods.LogProgress);
        //    tracker.OnComplete(Methods.LogProgress);
        //    tracker.Start();

        //    // gather the files in the assembly. all files in the ProductImport folder will be processed
        //    //var files = Assembly.GetExecutingAssembly().GetManifestResourceNames().ToList();

        //    var files = Directory.GetFiles("C:\\Repositories\\Utilities\\ProductFiles").ToList();

        //    //begin parsing the file and calling the API
        //    var products = new CreateResourcePipeline(_ocIntegrationClient, _settings);
        //    if (_settings.Live) await products.RunAsync(_ocIntegrationClient, files, tracker); else products.Run(files, tracker);

        //    tracker.Stop();
        //    tracker.Now(Methods.LogProgress);
        //    await tracker.CompleteAsync();
        //    Console.WriteLine($"Import complete: {DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}");
        //}

        private static async Task CreateProducts()
        {
            Console.WriteLine($"Beginning product create: {DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}");
            var tracker = new Tracker();
            tracker.Every(1.Minutes(), Methods.LogProgress);
            tracker.OnComplete(Methods.LogProgress);
            tracker.Start();

            var products = new CreateResourcePipeline(_ocIntegrationClient, _settings);
            await products.RunCreateProductsAsync(_ocIntegrationClient, tracker);

            tracker.Stop();
            tracker.Now(Methods.LogProgress);
            await tracker.CompleteAsync();
            Console.WriteLine($"Product create complete: {DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}");
        }

        private static async Task CreatePriceSchedules()
        {
            Console.WriteLine($"Beginning price schedule create: {DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}");
            var tracker = new Tracker();
            tracker.Every(1.Minutes(), Methods.LogProgress);
            tracker.OnComplete(Methods.LogProgress);
            tracker.Start();

            var priceSchedules = new CreateResourcePipeline(_ocIntegrationClient, _settings);
            await priceSchedules.RunCreatePriceSchedulesAsync(_ocIntegrationClient, tracker);

            tracker.Stop();
            tracker.Now(Methods.LogProgress);
            await tracker.CompleteAsync();
            Console.WriteLine($"price schedule create complete: {DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}");
        }

        private static async Task CreateInventoryRecords()
        {
            Console.WriteLine($"Beginning inventory record create: {DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}");
            var tracker = new Tracker();
            tracker.Every(1.Minutes(), Methods.LogProgress);
            tracker.OnComplete(Methods.LogProgress);
            tracker.Start();

            var inventoryRecords = new CreateResourcePipeline(_ocIntegrationClient, _settings);
            await inventoryRecords.RunCreateInventoryRecordsAsync(_ocIntegrationClient, tracker);

            tracker.Stop();
            tracker.Now(Methods.LogProgress);
            await tracker.CompleteAsync();
            Console.WriteLine($"Inventory record create complete: {DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}");
        }

        private static async Task AssignProducts()
        {
            Console.WriteLine($"Beginning product assignments: {DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}");
            var tracker = new Tracker();
            tracker.Every(1.Minutes(), Methods.LogProgress);
            tracker.OnComplete(Methods.LogProgress);
            tracker.Start();

            var productAssignments = new CreateResourcePipeline(_ocIntegrationClient, _settings);
            await productAssignments.RunProductAssignmentAsync(_ocIntegrationClient, tracker);

            tracker.Stop();
            tracker.Now(Methods.LogProgress);
            await tracker.CompleteAsync();
            Console.WriteLine($"product assignments complete: {DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}");
        }

        private static async Task SetDefaultPricing()
        {
            Console.WriteLine($"Beginning set default supplier pricing: {DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}");
            var tracker = new Tracker();
            tracker.Every(1.Minutes(), Methods.LogProgress);
            tracker.OnComplete(Methods.LogProgress);
            tracker.Start();

            var defaultPricing = new CreateResourcePipeline(_ocIntegrationClient, _settings);
            await defaultPricing.RunDefaultSupplierPricingAsync(_ocIntegrationClient, tracker);

            tracker.Stop();
            tracker.Now(Methods.LogProgress);
            await tracker.CompleteAsync();
            Console.WriteLine($"set default supplier pricing complete: {DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}");
        }
    }
}
