using zeco.autoapi.Extensions;

namespace zeco.autoapi.Json
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
