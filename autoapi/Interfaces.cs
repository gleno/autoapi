using System;
using System.Threading.Tasks;

namespace zeco.autoapi
{
    public interface IIdentifiable
    {
        Guid Id { get; }
        bool IsDeleted { get; set; }
        string TypeIdentity { get; }
    }

    public interface IUniqueAccessTokenProvider
    {
        Guid? GetResourceId(string token);
        Guid? GetSignedId(string token, ISecretResource resource);
        string MakeToken(ISecretResource resource);
        string RecoverToken(ISecretResource resource, Guid signedId);
    }

    public interface ISecretResource
    {
        Guid Id { get; }
        Guid Secret { get; }
    }

    public interface IGuidProvider
    {
        Guid Make();
    }

    public interface ICompressionService
    {
        byte[] Compress(byte[] buffer);
        byte[] Decompress(byte[] buffer);
    }

    public interface ISignatureService
    {
        Guid Sign(byte[] buffer);
        Guid Sign(string text);
    }

    public interface ISilo
    {
        Task<bool> Store(Guid signature, byte[] buffer, string mime = null);
        Task<byte[]> Retrieve(Guid signature);
        Task DeleteIfExists(Guid signature);
    }

}