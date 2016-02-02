namespace autoapi.CodeGeneration
{
    interface ICodeGenerator
    {
        string Filename { get; }

        string Generate<T, TUser>(string moduleName) where T : AutoApiDbContext<TUser> where TUser : AutoApiUser;
    }
}