using System;

namespace zeco.autoapi
{
    public interface IIdentifiable
    {
        bool IsDeleted { get; set; }

        string TypeIdentity { get; }
        
        Guid Id { get; }
    }

    public interface IUniqueAccessTokenProvider
    {
        string MakeToken(ISecretResource resource);
        Guid? GetResourceId(string token);
        Guid? GetSignedId(string token, ISecretResource resource);
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
}
