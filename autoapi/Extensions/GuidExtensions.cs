using System;

namespace zeco.autoapi.Extensions
{
    public static class GuidExtensions
    {
        public static string AsCompactString(this Guid guid)
        {
            var enc = Convert.ToBase64String(guid.ToByteArray())
                .Replace("/", "_")
                .Replace("+", "-");

            return enc.Substring(0, 22);
        }

        public static Guid Xor(this Guid a, Guid b)
        {
            return new Guid(a.ToByteArray().Xor(b.ToByteArray()));
        }
    }
}