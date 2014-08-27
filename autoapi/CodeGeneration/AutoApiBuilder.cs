using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CSharp;
using zeco.autoapi.Extensions;

namespace zeco.autoapi.CodeGeneration
{
    internal class AutoApiBuilder
    {
        #region Fields

        private readonly Type _baseControllerType;

        #endregion

        #region Constructors

        public AutoApiBuilder(Type baseControllerType)
        {
            _baseControllerType = baseControllerType;
        }

        #endregion

        #region Public Methods

        public Assembly GenerateAutoApiAssembly()
        {
            var source = GenerateSource();
            var assembly = GenerateAssembly(source);
            return assembly;
        }

        #endregion

        #region Private Helpers

        private Assembly GenerateAssembly(string source)
        {
            CodeDomProvider cpd = new CSharpCodeProvider();
            var parameters = new CompilerParameters
            {
                GenerateExecutable = false,
                GenerateInMemory = true,
                IncludeDebugInformation = Debugger.IsAttached,
                CompilerOptions = Debugger.IsAttached ? "" : "/optimize",
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
            var sb = new StringBuilder();

            sb.AppendLine("using " + _baseControllerType.Namespace + ";");
            sb.AppendLine("namespace gen.Controllers {");

            foreach (var type in GetAutoApiTypes())
            {
                var name = GetClassName(type);

                var cname = _baseControllerType.Name;
                cname = cname.Substring(0, cname.Length - 2);

                sb.AppendLine(string.Format("public class {0}Controller: {1}<{2}>  {{}}", name, cname, type.FullName));
            }

            sb.AppendLine("}");

            return sb.ToString();
        }

        private static string GetClassName(Type type)
        {
            return type.Name.Pluralize();
        }

        private IEnumerable<Assembly> GetAssemblyGraph(Assembly assembly = null)
        {
            assembly = assembly ?? Assembly.GetCallingAssembly();
            
            var references = assembly.GetReferencedAssemblies().ToDictionary(asm => asm.Name, asm => asm);

            var assemblies = new Dictionary<string, Assembly>();

            var keys = new HashSet<string>(references.Keys);

            foreach (var name in keys.ToArray())
            {
                var asm = Assembly.Load(references[name]);
                assemblies[name] = asm;

                foreach (var reference in asm.GetReferencedAssemblies())
                {
                    if (!assemblies.ContainsKey(reference.Name))
                    {
                        assemblies[reference.Name] = Assembly.Load(reference);
                    }
                }
            }

            var graph = new List<Assembly> {assembly};
            graph.AddRange(assemblies.Values);
            return graph;
        }

        public bool SpecializedTypeDoesNotExist(Type type)
        {
            var className = GetClassName(type);
            var specializationExists = _baseControllerType.Assembly.GetExportedTypes().Any(t => t.Name == className + "Controller");
            return !specializationExists;
        }

        private IEnumerable<Type> GetAutoApiTypes()
        {
            return _baseControllerType.Assembly.ExportedTypes
                .Where(t => t.GetCustomAttribute<AutoApiAttribute>() != null)
                .Where(SpecializedTypeDoesNotExist);
        }

        #endregion
    }
}