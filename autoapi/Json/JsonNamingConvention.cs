using autoapi.Extensions;

namespace autoapi.Json
{
    internal class JsonNamingConvention
    {
        public string GetObjectKeyName(string name)
        {
            return name.Decapitalize();
        }

        public string GetCollectionName(string name)
        {
            return name.Decapitalize().Pluralize();
        }
    }
}
