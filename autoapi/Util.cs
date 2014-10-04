using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using zeco.autoapi.Extensions;

namespace zeco.autoapi
{
    internal static class Util
    {
        private static readonly HashSet<string> _dupes;

        private static readonly Dictionary<string, Type> _outtypes;

        private static readonly object _lock = new object();

        static Util()
        {
            lock (_lock)
            {
                var types = AppDomain.CurrentDomain
                    .GetAssemblies()
                    .Where(assembly => !assembly.GlobalAssemblyCache)
                    .Where(assembly => !assembly.FullName.Contains("SignalR"))
                    .Select(assembly => assembly.GetTypes())
                    .SelectMany(t => t).ToArray();


                _dupes = new HashSet<string>(types
                    .Select(t => t.Name)
                    .GroupBy(t => t)
                    .Where(g => g.Count() > 1)
                    .SelectMany(n => n));

                var ets = Assembly.GetExecutingAssembly().GetExportedTypes();
                _outtypes = types.Except(ets).Where(t => ets.Any(et => et.IsAssignableFrom(t))).ToDictionary(t => t.FullName, t => t);
            }
        }

        internal static void RegisterAutoApiAssembly(Assembly assembly)
        {
            foreach (var type in assembly.GetExportedTypes())
                _outtypes[type.FullName] = type;
        }

        internal static bool HasAttributeWithProperty<T>(this Type t, Func<T, bool> selector) where T : Attribute
        {
            var attribute = t.GetCustomAttribute<T>();
            if (attribute == null) return false;
            return selector(attribute);
        }

        internal static bool IsUniqueByName(this Type type)
        {
            return !_dupes.Contains(type.Name);
        }

        internal static Type AsOutsideType(string typename)
        {
            if (_outtypes.ContainsKey(typename))
                return _outtypes[typename];
            return null;
        }

        internal static Type GetApiControllerFor(string typename)
        {
            if (!_outtypes.ContainsKey(typename)) return null;
            var type = _outtypes[typename];
            return _outtypes.Values
                .Where(typeof (ApiControllerBase).IsAssignableFrom)
                .SingleOrDefault(ct => ct.Name == type.Name.Pluralize() + "Controller");
        }

    }
}