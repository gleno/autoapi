using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using autoapi.Extensions;

namespace autoapi
{
    public static class Util
    {
        private static readonly HashSet<string> Dupes;

        private static readonly Dictionary<string, Type> Outtypes;

        private static readonly object Lock = new object();

        static Util()
        {
            lock (Lock)
            {
                var types = AppDomain.CurrentDomain
                    .GetAssemblies()
                    .Where(assembly => !assembly.GlobalAssemblyCache)
                    .Where(assembly => !assembly.FullName.Contains("SignalR"))
                    .Select(assembly => assembly.GetTypes())
                    .SelectMany(t => t).ToArray();


                Dupes = new HashSet<string>(types
                    .Select(t => t.Name)
                    .GroupBy(t => t)
                    .Where(g => g.Count() > 1)
                    .SelectMany(n => n));

                var ets = Assembly.GetExecutingAssembly().GetExportedTypes();
                Outtypes = types.Except(ets).Where(t => ets.Any(et => et.IsAssignableFrom(t))).ToDictionary(t => t.FullName, t => t);
            }
        }

        internal static void RegisterAutoApiAssembly(Assembly assembly)
        {
            foreach (var type in assembly.GetExportedTypes())
                Outtypes[type.FullName] = type;
        }

        public static bool HasAttributeWithProperty<T>(this Type t, Func<T, bool> selector) where T : Attribute
        {
            var attribute = t.GetCustomAttribute<T>();
            if (attribute == null) return false;
            return selector(attribute);
        }

        internal static bool IsUniqueByName(this Type type)
        {
            return !Dupes.Contains(type.Name);
        }

        internal static Type AsOutsideType(string typename)
        {
            if (Outtypes.ContainsKey(typename))
                return Outtypes[typename];
            return null;
        }

        internal static Type GetApiControllerFor(string typename)
        {
            if (!Outtypes.ContainsKey(typename)) return null;
            var type = Outtypes[typename];
            return Outtypes.Values
                .Where(typeof (ApiControllerBase).IsAssignableFrom)
                .SingleOrDefault(ct => ct.Name == type.Name.Pluralize() + "Controller");
        }

    }
}