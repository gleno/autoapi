using System;

namespace autoapi.Extensions
{
    public static class BufferExtensions
    {
        public static byte[] Xor(this byte[] a, byte[] b)
        {
            var n = Math.Min(a.Length, b.Length);
            var result = new byte[n];
            for (var i = 0; i < n; i++)
                result[i] = (byte)(a[i] ^ b[i]);
            return result;
        }

        public static bool Same(this byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            for(var i = 0; i < a.Length; ++i)
                if (a[i] != b[i]) return false;
            return true;
        }
    }
}
