using System;

namespace zeco.autoapi.Providers
{
    internal class ConcurrentGuidProvider : IGuidProvider
    {
        public Guid Make()
        {
            var buffer = new byte[16];
            ThreadLocalRandomProvider.Instance.NextBytes(buffer);
            return new Guid(buffer);
        }
    }
}