using System.Collections.Generic;

namespace autoapi.Extensions
{
    public static class DictionaryExtensions
    {
        public static TValue Get<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue defaultValue = default (TValue))
        {
            return dict.ContainsKey(key) ? dict[key] : defaultValue;
        }
    }
}
