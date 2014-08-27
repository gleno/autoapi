using System;
using zeco.autoapi.Extensions;

namespace zeco.autoapi.Providers
{

    public class SecureUniqueAccessTokenProvider : IUniqueAccessTokenProvider
    {
        public IGuidProvider GuidProvider { get; set; }

        public string MakeToken(ISecretResource resource)
        {
            return RecoverToken(resource, GuidProvider.Make());
        }

        public Guid? GetResourceId(string token)
        {
            if (token == null || token.Length < 44)
                return null;

            var a = token.Substring(0, 22).AsGuid();
            var b = token.Substring(22, 22).AsGuid();

            return a.Xor(b);
        }

        public Guid? GetSignedId(string token, ISecretResource resource)
        {
            if (token == null || token.Length != 44)
                return null;

            var a = token.Substring(0, 22).AsGuid();
            var b = token.Substring(22, 22).AsGuid();

            var t1 = resource.Secret.Xor(a);
            var t2 = resource.Secret.Xor(resource.Id).Xor(b);

            if (t1 != t2) return null;

            return t1;
        }

        public string RecoverToken(ISecretResource resource, Guid signedId)
        {
            var a = resource.Secret.Xor(signedId);
            var b = resource.Secret.Xor(resource.Id).Xor(signedId);

            return a.AsCompactString() + b.AsCompactString();
        }

    }
}
