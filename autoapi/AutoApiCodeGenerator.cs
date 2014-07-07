using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Reflection;
using zeco.autoapi.CodeGeneration;
using zeco.autoapi.Extensions;

namespace zeco.autoapi
{
    static class AutoApiCodeGenerator
    {
        private class DbContextTypeInformation
        {
            public Type ContextType { get; set; }
            public Type UserType { get; set; }
        }

        private static void GenScripts<TContext, TUser>(string directory) 
            where TContext : AutoApiDbContext<TUser> 
            where TUser : AutoApiUser
        {
            Directory.CreateDirectory(directory);

            var generators = typeof(ICodeGenerator).Assembly.GetTypes()
                .Where(t => typeof(ICodeGenerator).IsAssignableFrom(t))
                .Where(t => t.IsClass && !t.IsAbstract);

            var moduleName = Activator.CreateInstance<TContext>().ModuleName;

            foreach (var type in generators)
            {
                var generator = (ICodeGenerator)Activator.CreateInstance(type);
                var filename = Path.Combine(directory, string.Format("{0}.{1}", moduleName, generator.Filename));
                var source = generator.Generate<TContext, TUser>(moduleName);
                File.WriteAllText(filename, source);
            }
        }

        public static void Main(string[] args)
        {
            string assemblyPath, directory;
            if (!ValidateArguments(args, out assemblyPath, out directory))
            {
                Console.WriteLine("Invalid arguments.");
                return;
            }

            CreateScripts(assemblyPath, directory);
        }

        private static bool ValidateArguments(string[] args, out string assemblyPath, out string directory)
        {
            directory = assemblyPath = null;
            if (args.Length != 2) return false;

            assemblyPath = args[0];
            if (!IsAssemblyPath(assemblyPath)) Console.WriteLine("{0} is not an assembly.", assemblyPath);

            directory = args[1];
            if (!IsValidDirectory(directory)) Console.WriteLine("{0} is not a valid directory.", directory);
            return true;
        }

        private static bool IsValidDirectory(string directory)
        {
            try
            {
                Path.GetFullPath(directory);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void CreateScripts(string assemblyPath, string directory)
        {
            var contexts = GetAutoApiDbContexts(assemblyPath);

            var mname = ObjectExtensions.NameOf(n => GenScripts<AutoApiDbContext<AutoApiUser>, AutoApiUser>(null));

            var method = typeof (AutoApiCodeGenerator).GetMethod(mname, BindingFlags.NonPublic | BindingFlags.Static);

            foreach (var context in contexts)
            {
                method.MakeGenericMethod(context.ContextType, context.UserType)
                    .Invoke(null, new object[] {directory});
            }
        }

        private static bool IsAssemblyPath(string path)
        {
            try
            {
                AssemblyName.GetAssemblyName(path);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static IEnumerable<DbContextTypeInformation> GetAutoApiDbContexts(string assemblyDirectory)
        {
            return Assembly.LoadFile(assemblyDirectory).GetTypes()
                .Select(t => new DbContextTypeInformation {ContextType = t, UserType = GetUserType(t)})
                .Where(sig => sig.UserType != null);
        }

        private static Type GetUserType(Type type)
        {
            if (! typeof (DbContext).IsAssignableFrom(type))
                return null;

            do
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof (AutoApiDbContext<>))
                    return type.GetGenericArguments()[0];

                type = type.BaseType;

            } while (type != null);

            return null;
        }
    }
}
