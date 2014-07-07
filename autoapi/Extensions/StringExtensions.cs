using System;
using System.Data.Entity.Design.PluralizationServices;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using zeco.autoapi.Json;

namespace zeco.autoapi.Extensions
{
    public static class StringExtensions
    {
        private static readonly PluralizationService _pluralizationService = 
            PluralizationService.CreateService(CultureInfo.GetCultureInfo("en-us"));

        public static string Decapitalize(this string str)
        {
            if (str.Length < 2)
                return str.ToLowerInvariant();

            return char.ToLowerInvariant(str[0]) + str.Substring(1);
        }

        public static dynamic AsJson(this string str)
        {
            if (String.IsNullOrWhiteSpace(str))
                return new JsObject();

            return JsonConvert.DeserializeObject<JsObject>(str, new ExpandoObjectConverter());
        }

        public static dynamic AsJsonArray(this string str)
        {
            var wrap = "{array:" + str + "}";
            return AsJson(wrap).array;
        }

        public static string Pluralize(this string str)
        {
            return _pluralizationService.Pluralize(str);
        }

        public static Guid AsGuid(this string encoded)
        {
            encoded = encoded.Replace("_", "/").Replace("-", "+");
            var buffer = Convert.FromBase64String(encoded + "==");
            return new Guid(buffer);
        }
    }
}