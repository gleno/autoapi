using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using autoapi.Extensions;
using Microsoft.CSharp;

namespace autoapi.CodeGeneration
{
    class AutoApiBuilder
    {

        private readonly Type _baseControllerType;

        public AutoApiBuilder(Type baseControllerType)
        {
            _baseControllerType = baseControllerType;
        }

        public Assembly GenerateAutoApiAssembly()
        {
            var source = GenerateSource();
            return source == null ? null : GenerateAssembly(source);
        }

        private bool SpecializedTypeDoesNotExist(Type type)
        {
            var className = GetClassName(type);
            var specializationExists = _baseControllerType.Assembly.GetExportedTypes().Any(t => t.Name == className + "Controller");
            return !specializationExists;
        }

        private Assembly GenerateAssembly(string source)
        {
            CodeDomProvider cpd = new CSharpCodeProvider();
            var parameters = new CompilerParameters
            {
                GenerateExecutable = false,
                GenerateInMemory = true,
                IncludeDebugInformation = this.IsDebug(),
                CompilerOptions = this.IsDebug() ? "" : "/optimize"
            };

            var references = GetAssemblyGraph(_baseControllerType.Assembly)
                .Select(a => a.Location)
                .ToArray();

            parameters.ReferencedAssemblies.AddRange(references);

            var compilerResults = cpd.CompileAssemblyFromSource(parameters, source);

            if (compilerResults.Errors.Count > 0)
            {
                throw new Exception("Error");
            }

            return compilerResults.CompiledAssembly;
        }

        private string GenerateSource()
        {
            var types = GetAutoApiTypes();
            if (!types.Any()) return null;

            var sb = new StringBuilder();

            sb.AppendLine("using " + _baseControllerType.Namespace + ";");
            sb.AppendLine("namespace gen.Controllers {");

            foreach (var type in types)
            {
                var name = GetClassName(type);

                var cname = _baseControllerType.Name;
                cname = cname.Substring(0, cname.Length - 2);

                sb.AppendLine($"public class {name}Controller: {cname}<{type.FullName}>  {{}}");
            }

            sb.AppendLine("}");

            return sb.ToString();
        }

        private IEnumerable<Assembly> GetAssemblyGraph(Assembly assembly = null)
        {
            assembly = assembly ?? Assembly.GetCallingAssembly();

            var references = new Dictionary<string, AssemblyName>();
            foreach (var name in assembly.GetReferencedAssemblies())
                references[name.Name] = name;

            var assemblies = new Dictionary<string, Assembly>();
            foreach (var name in references.Keys)
            {
                var asm = assemblies[name] = Assembly.Load(references[name]);
                foreach (var reference in asm.GetReferencedAssemblies())
                    if (!assemblies.ContainsKey(reference.Name))
                        assemblies[reference.Name] = Assembly.Load(reference);
            }

            var graph = new List<Assembly> {assembly};
            graph.AddRange(assemblies.Values);
            return graph;
        }

        private Type[] GetAutoApiTypes()
        {
            return _baseControllerType.Assembly.ExportedTypes
                .Where(t => t.GetCustomAttribute<AutoApiAttribute>() != null)
                .Where(SpecializedTypeDoesNotExist)
                .ToArray();
        }

        private static string GetClassName(Type type)
        {
            return type.Name.Pluralize();
        }
    }
}