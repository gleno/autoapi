using System;

namespace autoapi.Providers
{
    public class ConcurrentGuidProvider : IGuidProvider
    {
        public Guid Make()
        {
            var buffer = new byte[16];
            ThreadLocalRandomProvider.Instance.NextBytes(buffer);
            return new Guid(buffer);
        }
    }
}