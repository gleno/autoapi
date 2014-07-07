namespace zeco.autoapi.CodeGeneration
{
    public interface ICodeGenerator
    {
        #region Public Properties

        string Filename { get; }

        #endregion

        #region Interface

        string Generate<T, TUser>(string moduleName) where T : AutoApiDbContext<TUser> where TUser : AutoApiUser;

        #endregion
    }
}