using System;
using System.Data.Entity.Design.PluralizationServices;
using System.Globalization;
using System.Linq;
using System.Text;
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
        
        public static string Capitalize(this string str)
        {
            if (str.Length < 2)
                return str.ToUpperInvariant();

            return char.ToUpperInvariant(str[0]) + str.Substring(1);
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

        public static string MD5(this string input, Encoding encoding = null)
        {
            encoding = encoding ?? Encoding.UTF8;
            var buffer = input.Serialize(encoding);
            return string.Join("", System.Security.Cryptography.MD5.Create().ComputeHash(buffer).Select(hb => hb.ToString("X2")));
        }

        public static byte[] Serialize(this string input, Encoding encoding = null)
        {
            encoding = encoding ?? Encoding.UTF8;
            return encoding.GetBytes(input);
        }
    }
}