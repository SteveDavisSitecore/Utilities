using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using OrderCloud.SDK;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;
using System.Collections;
using System.Reflection;

namespace Utilities
{
    public class BokusConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Product<BokusXp>);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            Product<BokusXp> product = new Product<BokusXp>()
            {
                xp = new BokusXp()
            };
            JObject obj = JObject.Load(reader);

            //product.ID = ((string)obj["sku"]).ToSafeID();
            product.ID = Guid.NewGuid().ToString();
            product.DefaultPriceScheduleID = "DefaultPrice";
            product.Returnable = true;
            product.AllSuppliersCanSell = false;
            product.Active = true;
            product.Description = (string)obj["description"].ToString().TruncateLongString(1999);
            product.Name = (string)obj["title"].ToString().TruncateLongString(99);
            product.QuantityMultiplier = 1;
            product.ShipHeight = (int?)obj["height"];
            product.ShipWeight = (int?)obj["weight"];
            product.ShipLength = (int?)obj["length"];
            product.ShipWidth = (int?)obj["width"];

            product.xp.publisher = (string)obj["publisher"];
            product.xp.illustrations = (string)obj["illustrations"];
            product.xp.onix_code = (string)obj["onix_code"];
            product.xp.num_pages = (string)obj["num_pages"];
            product.xp.language_code = (string)obj["language_code"];
            product.xp.edition = (string)obj["edition"];
            product.xp.content = (string)obj["content"];
            product.xp.authors = obj["authors"].ToObject<List<string>>(serializer);
            product.xp.binding = (string)obj["binding"];
            product.xp.language = (string)obj["language"];

            //var sub = obj["subjects"].ToObject<Subject[]>(serializer);
            //foreach (var s in sub)
            //{
            //    if (s.subject_level1 != null)
            //        product.xp.categories.Add(new Category() { code = s.subject_level1.code.ToSafeID(), name = s.subject_level1.name });
            //    if (s.subject_level2 != null)
            //        AddProperty(product.xp.facets, s.subject_level2.code, s.subject_level2.name);
            //    if (s.subject_level3 != null)
            //        AddProperty(product.xp.facets, s.subject_level3.code, s.subject_level3.name);
            //    if (s.subject_level4 != null)
            //        AddProperty(product.xp.facets, s.subject_level4.code, s.subject_level4.name);
            //}

            return product;
        }

        public override bool CanWrite => base.CanWrite;
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public static void AddProperty(ExpandoObject expando, string code, string name)
        {
            // ExpandoObject supports IDictionary so we can extend it like this
            var expandoDict = expando as IDictionary<string, object>;
            if (expandoDict.ContainsKey(code))
                expandoDict[code] = name;
            else
                expandoDict.Add(code, name);
        }
    }
}
