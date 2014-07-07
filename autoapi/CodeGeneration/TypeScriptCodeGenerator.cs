using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using zeco.autoapi.Extensions;

namespace zeco.autoapi.CodeGeneration
{
    abstract class TypeScriptCodeGenerator : ICodeGenerator
    {
        int _depth;
        StringBuilder _builder;
        bool _backtraceNext;
        private Assembly _assembly;

        public abstract string Filename { get; }

        protected Type ContextType { get; set; }

        public string Generate<TContext, TUser>(string moduleName) where TContext : AutoApiDbContext<TUser> where TUser : AutoApiUser
        {

            ModuleName = moduleName;
            ContextType = typeof (TContext);

            _assembly = typeof(TContext).Assembly;
            _depth = 0;
            _builder = new StringBuilder();
            GenerateInternal();
            return _builder.ToString();
        }

        public string ModuleName { get; private set; }

        protected abstract void GenerateInternal();

        protected Type[] GetDatatypes()
        {
            var datatypes = new List<Type>();

            foreach (var type in _assembly.GetExportedTypes())
            {
                var isDatabaseItem = typeof (IIdentifiable).IsAssignableFrom(type);
                var isFinal = !(type.IsGenericType || type.IsAbstract);

                if (isDatabaseItem && isFinal)
                    if (type.GetCustomAttribute<AutoApiAttribute>() != null)
                        datatypes.Add(type);
            }

            return datatypes.ToArray();
        }

        protected void Tab(Action action, int depth = 1)
        {
            _depth += depth;
            action();
            _depth -= depth;
        }

        protected void Var(string var, string value)
        {
            Statement("var " + var + " = " + value + ";");
        }

        protected void JObject(string var, IDictionary<string, Action> props)
        {

            Statement("var " + var + " = {");
            ++_depth;

            var keys = props.Keys.ToArray().ToArray();

            for (int index = 0; index < keys.Length; index++)
            {
                var key = keys[index];
                Statement("'" + key + "' : ");

                ++_depth;
                _backtraceNext = true;
                props[key]();
                if (index < keys.Length - 1)
                    Statement(", ", true);
                --_depth;
            }
            --_depth;
            Statement("};");
        }

        protected void Scope(string str, Action action)
        {
            Statement(str + " ");
            _backtraceNext = true;
            Statement("{");
            Tab(action);
            Statement("}");

        }

        protected void Scope(string str, string action)
        {
            Scope(str, Wrap(action));
        }

        protected void Function(string name, Action action, params string[] parameters)
        {
            var plist = string.Join(", ", parameters);
            Scope(string.Format("function {0}({1})", name, plist), action);
            Statement();
        }

        protected void For(string var, string collection, Action action)
        {
            Scope(string.Format("for(var {0} in {1})", var, collection), action);
        }        
        
        protected void For(string var, string collection, string action)
        {
            For(var, collection, Wrap(action));
        }

        protected void ForEach(string item, string collection, Action action, string idx = "idx")
        {
            For(idx, collection, () =>
            {
                Var(item, string.Format("{0}[{1}]", collection, idx));
                action();
            });
        }        
        
        protected void ForEach(string item, string collection, string action, string idx = "idx")
        {
            ForEach(item, collection, Wrap(action), idx);
        }

        protected void If(string condition, Action action)
        {
            Scope(string.Format("if({0})", condition), action);
        }

        protected void If(string condition, string action)
        {
            If(condition, Wrap(action));
        }

        protected void ElseIf(string condition, Action action)
        {
            Scope(string.Format("else if({0})", condition), action);
        }

        protected void ElseIf(string condition, string action)
        {
            ElseIf(condition, Wrap(action));
        }        
        
        protected void Else(Action action)
        {
            Scope(string.Format("else"), action);
        }

        protected void Else(string action)
        {
            Else(Wrap(action));
        }

        Action Wrap(string action)
        {
            return () => Statement(action);
        }

        protected void Return(string var)
        {
            Statement("return " + var + ";");
        }

        protected void Return(Action action)
        {
            Statement("return ");
            _backtraceNext = true;
            action();
        }

        protected void Statement(int count = 1)
        {
            for (var i = 0; i < count; i++)
                _builder.AppendLine();
        }

        protected void Statement(string str, bool backtrace = false)
        {
            var prefix = string.Join("", Enumerable.Repeat("\t", _depth));

            backtrace = backtrace || _backtraceNext;

            if (backtrace)
            {
                prefix = string.Empty;
                _builder.Length = _builder.Length - 2; //CLRF
            }

            if (_backtraceNext)
                _backtraceNext = false;

            _builder.AppendLine(prefix + str);
        }

        protected string GetInterfaceName(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;

            if (type.IsGenericType)
            {
                if (type.GetGenericTypeDefinition() == typeof (ICollection<>))
                    //return GetInterfaceName(type.GetGenericArguments()[0]) + "[]";
                    return "string[]"; //Only guids
                return "any";
            }

            if (type == typeof(bool))
                return "boolean";
            if (type == typeof(Guid))
                return "string";
            if (type == typeof(string))
                return "string";
            if (type == typeof(int) || type == typeof(double))
                return "number";
            if (type == typeof(DateTime))
                return "Date";

            return "I" + type.Name;
        }

        protected string GetName(Type type)
        {
            return type.Name.Decapitalize();
        }

        protected string GetPluralName(Type type)
        {
            return GetName(type).Pluralize();
        }

        protected string Route(Type type)
        {
            return "/api/" + type.Name.ToLowerInvariant().Pluralize() + "/";
        }

        protected void Raw(string str)
        {
            _builder.Append(str);
        }
    }
}