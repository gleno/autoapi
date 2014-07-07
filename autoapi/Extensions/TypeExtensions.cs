using System;
using System.Linq;
using System.Reflection;

namespace zeco.autoapi.Extensions
{
    public static class TypeExtensions
    {
        public static bool HasAttributeWithProperty<T>(this Type t, Func<T, bool> selector) where T : Attribute
        {
            var attribute = t.GetCustomAttribute<T>();
            if (attribute == null) return false;
            return selector(attribute);
        }

        public static bool IsUniqueByName(this Type type)
        {
            return AppDomain.CurrentDomain
                .GetAssemblies()
                .Select(assembly => assembly.GetTypes())
                .SelectMany(t => t)
                .Count(t => t.Name == type.Name) < 2;
        }
    }
}