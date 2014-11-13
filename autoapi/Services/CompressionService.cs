using System.IO;
using System.IO.Compression;

namespace zeco.autoapi.Services
{
    public class NullCompressionService : ICompressionService
    {
        public byte[] Compress(byte[] buffer)
        {
            return buffer;
        }

        public byte[] Decompress(byte[] buffer)
        {
            return buffer;
        }
    }

    public class SimpleCompressionService : ICompressionService
    {
        #region Public Methods

        public static byte[] Transform(byte[] buffer, MemoryStream ms, CompressionMode op)
        {
            using (var gz = new GZipStream(ms, op, true))
            {
                gz.Write(buffer, 0, buffer.Length);
                return ms.ToArray();
            }
        }

        public byte[] Compress(byte[] buffer)
        {
            using (var ms = new MemoryStream())
            {
                return Transform(buffer, ms, CompressionMode.Compress);
            }
        }

        public byte[] Decompress(byte[] buffer)
        {
            using (var ms = new MemoryStream())
            {
                return Transform(buffer, ms, CompressionMode.Decompress);
            }
        }

        #endregion
    }
}
