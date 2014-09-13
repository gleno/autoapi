using System.Collections.Generic;

namespace zeco.autoapi.Extensions
{
    public static class DictionaryExtensions
    {
        public static TValue Get<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue defaultValue = default (TValue))
        {
            if (dict.ContainsKey(key))
                return dict[key];
            return defaultValue;
        }
    }
}
