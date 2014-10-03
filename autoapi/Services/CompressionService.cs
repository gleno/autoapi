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

        public byte[] Compress(byte[] buffer)
        {
            using (var input = new MemoryStream(buffer))
            using (var output = new MemoryStream())
            {
                using (var gs = new GZipStream(output, CompressionMode.Compress))
                    input.CopyTo(gs);

                return output.ToArray();
            }
        }

        public byte[] Decompress(byte[] buffer)
        {
            using (var input = new MemoryStream(buffer))
            using (var output = new MemoryStream())
            {
                using (var gs = new GZipStream(input, CompressionMode.Decompress))
                    gs.CopyTo(output);
                return output.ToArray();
            }
        }

        #endregion
    }
}
