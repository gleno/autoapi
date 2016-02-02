using System;

namespace autoapi.Extensions
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

        public static Guid FlipEndian(this Guid guid)
        {
            var flipped = new byte[16];
            var original = guid.ToByteArray();

            for (var i = 8; i < 16; i++)
                flipped[i] = original[i];

            flipped[3] = original[0];
            flipped[2] = original[1];
            flipped[1] = original[2];
            flipped[0] = original[3];
            flipped[5] = original[4];
            flipped[4] = original[5];
            flipped[6] = original[7];
            flipped[7] = original[6];

            return new Guid(flipped);
        }

        public static byte[] AsOrderedBytes(this Guid guid)
        {
            return guid.FlipEndian().ToByteArray();
        }

        public static Guid Xor(this Guid a, Guid b)
        {
            return new Guid(a.ToByteArray().Xor(b.ToByteArray()));
        }
    }
}