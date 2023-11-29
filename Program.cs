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

            Console.WriteLine("First step is to configure the buyer environment. Type Y to continue, N to skip");
            var setup_buyer = Console.ReadLine();
            if (setup_buyer.ToLower() == "y")
            {
                await SetupBuyer();
            }

            Console.WriteLine("Next step is to import products embedded in assembly. Type Y to continue, N to skip");
            var import_products = Console.ReadLine();
            if (import_products.ToLower() == "y")
            {
                await ImportProducts();
            }
        }

        private static async Task SetupBuyer()
        {
            var buyer = new ShopperImportPipeline(_ocIntegrationClient, _settings);
            await buyer.RunAsync();
        }

        private static async Task ImportProducts()
        {
            // just some on screen tracking information
            Console.WriteLine($"Beginning import: {DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}");
            var tracker = new Tracker();
            tracker.Every(15.Minutes(), Methods.LogProgress);
            tracker.OnComplete(Methods.LogProgress);
            tracker.Start();

            // gather the files in the assembly. all files in the ProductImport folder will be processed
            //var files = Assembly.GetExecutingAssembly().GetManifestResourceNames().ToList();

            var files = Directory.GetFiles("C:\\Repositories\\Bokus\\ProductFiles").ToList();

            //begin parsing the file and calling the API
            var products = new ProductImportPipeline(_ocIntegrationClient, _settings);
            if (_settings.Live) await products.RunAsync(_ocIntegrationClient, files, tracker); else products.Run(files, tracker);

            tracker.Stop();
            tracker.Now(Methods.LogProgress);
            await tracker.CompleteAsync();
            Console.WriteLine($"Import complete: {DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}");
        }
    }
}
