using System;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using zeco.autoapi.Extensions;

namespace zeco.autoapi.Services
{
    public class ShaSignatureService : ISignatureService
    {
        public Guid Sign(byte[] buffer)
        {
            var sha1 = SHA1.Create();
            var hash = sha1.ComputeHash(buffer);
            if (hash.Length < 16)
                throw new SecurityException("Invalid hash length");
            return new Guid(hash.Take(16).ToArray());
        }

        public Guid Sign(string text)
        {
            return Sign(text.Serialize());
        }
    }
}