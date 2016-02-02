using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using autoapi.Json;
using Newtonsoft.Json;

namespace autoapi.Extensions
{
    public static class ObjectExtensions
    {
        public static string ToJson(this object graph)
        {
            return JsonConvert.SerializeObject(graph);
        }        
        
        public static string ToSafeJson(this object graph)
        {

            var builder = new StringBuilder();

            using (var writer = new JsonTextWriter(new StringWriter(builder)))
                new JsonSerializer { ContractResolver = new JsonContractResolver() }.Serialize(writer, graph);

            return builder.ToString();
        }

        public static bool IsDebug(this object any)
        {
#if DEBUG
            return true;
#else
            return false;
#endif
        }

        public static bool IsAutoProperty(this PropertyInfo prop)
        {
            if (!prop.CanWrite || !prop.CanRead)
                return false;

            return prop.DeclaringType != null && prop.DeclaringType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                .Any(f => f.Name.Contains("<" + prop.Name + ">"));
        }

        public static bool IsNullableValueType(this Type type)
        {
            if (!type.IsValueType) return false;
            if (Nullable.GetUnderlyingType(type) != null) return true;
            return false;
        }

        public static bool IsCollection(this Type type)
        {
            if (type.IsGenericType)
                if (type.GetGenericTypeDefinition() == typeof(ICollection<>))
                {
                    var coltype = type.GetGenericArguments()[0];
                    if (typeof(IIdentifiable).IsAssignableFrom(coltype))
                        return true;
                }

            return false;
        }


        public static string NameOf<T, TQ>(this T obj, Expression<Func<T, TQ>> accessor)
        {
            return NameOf(accessor);
        }

        public static string NameOf<T>(this T obj, Expression<Action<T>> accessor)
        {
            return NameOf(accessor);
        }        
        
        public static string NameOf(Expression<Action<object>> accessor)
        {
            return NameOf((LambdaExpression) accessor);
        }

        public static string NameOf<T, TQ>(Expression<Func<T, TQ>> accessor)
        {
            return NameOf((LambdaExpression) accessor);
        }

        public static string NameOf(LambdaExpression memberSelector)
        {
            var body = memberSelector.Body;

            while (true)
            {
                switch (body.NodeType)
                {
                    case ExpressionType.Parameter:
                        return ((ParameterExpression)body).Name;

                    case ExpressionType.MemberAccess:
                        return ((MemberExpression)body).Member.Name;

                    case ExpressionType.Call:
                        return ((MethodCallExpression)body).Method.Name;

                    case ExpressionType.Convert:
                    case ExpressionType.ConvertChecked:
                        body = ((UnaryExpression)body).Operand;
                        break;

                    case ExpressionType.Invoke:
                        body = ((InvocationExpression)body).Expression;
                        break;

                    case ExpressionType.ArrayLength:
                        return "Length";

                    default:
                        const string msg = "The specified accessor is invalid.";
                        throw new FormatException(msg);
                }
            }
        }


    }


}



