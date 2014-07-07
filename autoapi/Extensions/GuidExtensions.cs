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
            var ab = a.ToByteArray();
            var bb = b.ToByteArray();

            var result = new byte[16];
            for (var i = 0; i < 16; i++)
                result[i] = (byte)(ab[i] ^ bb[i]);

            return new Guid(result);
        }
    }
}