using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using OrderCloud.SDK;
using Utilities.Helpers;

namespace Utilities
{
    public class Utilities
    {
        private static IOrderCloudClient _ocIntegrationClient;
        public static AppSettings _settings;
        private const int PRODUCT_COUNT = 100;

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

            Console.WriteLine("Next step is to import products embedded in assembly. Type Y to continue, N to skip");
            var import_products = Console.ReadLine();
            if (import_products.ToLower() == "y")
            {
                await ImportProducts();
            }
        }

        private static async Task ImportProducts()
        {
            // get initial product count
            var initialTotalCount = await Methods.RetrieveMetaTotalCount(_ocIntegrationClient);

            // just some on screen tracking information
            Console.WriteLine($"Beginning import: {DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}");
            var tracker = new Tracker();
            tracker.Every(1.Minutes(), Methods.LogProgress);
            tracker.OnComplete(Methods.LogProgress);
            tracker.Start();

            // begin calling the API
            var products = new ProductImportPipeline(_ocIntegrationClient, _settings);
            var lastID = await products.RunAsync(_ocIntegrationClient, tracker, PRODUCT_COUNT);

            tracker.Stop();
            tracker.Now(Methods.LogProgress);
            await tracker.CompleteAsync();
            Console.WriteLine($"API Import complete: {DateTime.Now.ToShortDateString()} {DateTime.Now.ToLongTimeString()}");

            await PollForLastProduct(lastID, initialTotalCount);

            // final check that total metacount matches initial total + product count processed
            var newCount = await Methods.RetrieveMetaTotalCount(_ocIntegrationClient);
            if (newCount != initialTotalCount + PRODUCT_COUNT)
            {
                throw new Exception(
                    $"Meta TotalCount: {newCount} does not equal initial count: {initialTotalCount} + product count: {PRODUCT_COUNT}");
            }
        }

        private static async Task PollForLastProduct(string productID, int initialTotalCount)
        {
            var results = await Methods.ListProductsWithLastIDFilter(_ocIntegrationClient, productID);

            // poll every 1 second until last product ID shows up in product list (indexed in Elasticsearch)
            while (!results.Items.Any())
            {
                await Task.Delay(1000);
                results = await Methods.ListProductsWithLastIDFilter(_ocIntegrationClient, productID);
            }

            Console.WriteLine($"Last Product ID indexed in Elasticsearch: {DateTime.Now.ToShortDateString()} {DateTime.Now.ToLongTimeString()}");
        }
    }
}
