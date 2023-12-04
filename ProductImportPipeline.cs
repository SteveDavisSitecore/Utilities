using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OrderCloud.SDK;
using Utilities.Helpers;

namespace Utilities
{
    public class ProductImportPipeline
    {
        private readonly IOrderCloudClient _oc;
        private readonly AppSettings _settings;
        private const int TEST_BATCH = 10000;
        private const int SKIP_COUNT = 100000;

        public ProductImportPipeline(IOrderCloudClient oc, AppSettings settings)
        {
            _oc = oc;
            _settings = settings;
        }

        public void Run(List<string> files, Tracker tracker)
        {
            foreach (var file in files)
            {
                var products = ProductMapping(file, tracker);
                Methods.WritePriceSchedule(_settings.CatalogID); // using catalog ID for conevenience
                Methods.WriteCatalog(_settings.CatalogID);
                Methods.WriteCategory(Categories(products));
                Methods.WriteFacet(Facets(products));
                Methods.WriteProducts(products, file);
            }
        }

        public async Task FixFacets(IOrderCloudClient oc)
        {
            for(var i = 1; i <= 25; i++)
            {
                var list = await oc.ProductFacets.ListAsync(page: i, pageSize: 100);
                await Throttler.RunAsync(list.Items, 10, 100, async facet =>
                {
                    await oc.ProductFacets.PatchAsync(facet.ID, new PartialProductFacet()
                    {
                        XpPath = $"facets.{facet.ID}",
                        MinCount = 100
                    });
                    Console.WriteLine($"{facet.ID}");
                });
            }
        }

        public async Task DeleteFacets(IOrderCloudClient oc)
        {
            for (var i = 1; i <= 25; i++)
            {
                var list = await oc.ProductFacets.ListAsync(page: i, pageSize: 100);
                await Throttler.RunAsync(list.Items, 10, 100, async facet =>
                {
                    await oc.ProductFacets.DeleteAsync(facet.ID);
                });
            }
        }

        public async Task RunAsync(IOrderCloudClient oc, List<string> files, Tracker tracker)
        {
            Console.WriteLine("starting");
            //await Methods.PutPriceSchedule(oc);

            foreach (var file in files)
            {
                Console.WriteLine($"Staring file {file}");
                //var products = ProductMapping(file, tracker);
                var products = ProductIDs(file);
                using (var stream = File.CreateText("C:\\Repositories\\Utilities\\Log\\ids.txt"))
                    foreach(var s in products)
                    {
                        stream.WriteLine(s);
                    }

                tracker.ItemsDiscovered(products.Count);

                //Console.WriteLine($"PUT categories");
                //await Throttler.RunAsync(Categories(products), 10, 1000, async f =>
                //{
                //    await Methods.PutCategory(oc, f, _settings.CatalogID);
                //});
                //Console.WriteLine($"PUT facets");
                //await Throttler.RunAsync(Facets(products), 10, 1000, async f =>
                //{
                //    await Methods.PutFacet(oc, f);
                //});
                Console.WriteLine("PUT products");
                await Throttler.RunAsync(products, 20, 1000, async p =>
                {
                    //await Methods.PutProducts(oc, p, _settings.CatalogID, tracker);
                    //await Methods.PatchProducts(oc, p, _settings.CatalogID, tracker);
                    await Methods.PutPriceSchedules(oc, p, tracker);
                });
                await Throttler.RunAsync(products, 20, 1000, async p =>
                {
                    await Methods.PatchProducts(oc, p, tracker);
                });
                Console.WriteLine("Complete");
            }
        }

        private List<OrderCloud.SDK.Category> Categories(HashSet<Product<BokusXp>> hash)
        {
            //var grouped = from item in hash
            //              from cat in item.xp.categories
            //              select cat;
            //return grouped.DistinctBy(c => new { c.code, c.name }).Select(c => new OrderCloud.SDK.Category()
            //{
            //    Active = true,
            //    Description = c.name,
            //    ID = c.code.ToSafeID(),
            //    Name = c.name
            //}).ToList();
            throw new NotImplementedException();
        }

        private List<ProductFacet> Facets(HashSet<Product<BokusXp>> hash)
        {
            throw new NotImplementedException();
            //var list = new List<ProductFacet>();
            //var grouped = hash.GroupBy(p => p.xp.facets, (key, group) => group.First().xp.facets).ToList();
            //list.AddRange(from item in grouped
            //              from KeyValuePair<string, object> facet in item
            //              let f = new ProductFacet()
            //              {
            //                  ID = facet.Key.ToSafeID(),
            //                  Name = facet.Value.ToString(),
            //                  XpPath = facet.Key
            //              }
            //              select f);
            //var unique = list.DistinctBy(f => new { f.ID, f.Name }).Select(f => new ProductFacet()
            //{
            //    ID = f.ID,
            //    Name = f.Name ?? "Other",
            //    XpPath = f.ID
            //}).ToList();
            //return unique;
        }

        private List<string> ProductIDs(string file)
        {
            return new HashSet<string>(File.ReadLines(file))
                .Skip(SKIP_COUNT)
                .Take(TEST_BATCH)
                .ToList();
        }

        private HashSet<Product<BokusXp>> ProductMapping(string file, Tracker tracker)
        {
            var list = new HashSet<Product<BokusXp>>();
            using (System.IO.Stream fs = new FileStream(file, FileMode.Open, FileAccess.Read))
            using (StreamReader streamReader = new StreamReader(fs))
            using (JsonTextReader reader = new JsonTextReader(streamReader))
            {
                reader.SupportMultipleContent = true;
                var serializer = new JsonSerializer();
                serializer.Converters.Add(new BokusConverter());
               
                while (reader.Read())
                {
                    if (reader.TokenType == JsonToken.StartObject)
                    {
                        var t = serializer.Deserialize<Product<BokusXp>>(reader);
                        list.Add(t);
                    }
                }
            }
            return list;

            // worked great with smaller files
            //using StreamReader r = new StreamReader(file);
            //string json = r.ReadToEnd();

            //HashSet<Product<BokusXp>> list = JsonConvert.DeserializeObject<HashSet<Product<BokusXp>>>(json, new JsonSerializerSettings()
            //{
            //    Culture = CultureInfo.InvariantCulture,
            //    Converters = new List<JsonConverter> { new BokusConverter() }
            //});
            //return list;
        }
    }
}
