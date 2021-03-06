using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using autoapi.Extensions;
using Microsoft.AspNet.Identity.EntityFramework;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace autoapi.Json
{
    public class JsonContractResolver : DefaultContractResolver
    {

        public static bool AlwaysUseNativeFormatting { get; set; } = false;

        private class ItemKeyProvider : IValueProvider
        {
            public PropertyInfo Property { get; set; }

            public void SetValue(object target, object value)
            {
                throw new NotSupportedException();
            }

            public object GetValue(object target)
            {
                var value = (IEnumerable<IIdentifiable>)Property.GetMethod.Invoke(target, new object[0]);
                if (value == null) return new Guid[0];

                return value
                    .Where(t => !t.IsDeleted)
                    .Select(t => t.Id)
                    .ToArray();
            }

            public ItemKeyProvider(PropertyInfo property)
            {
                Property = property;
            }
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);

            var isAutoType = typeof (IIdentifiable).IsAssignableFrom(member.DeclaringType);
            var isUserType = typeof(IdentityUser<Guid, AutoApiUserLogin, AutoApiUserRole, AutoApiUserClaim>).IsAssignableFrom(member.DeclaringType);
            
            var attr = member.GetCustomAttribute<AutoPropertyAttribute>();
            if (attr == null)
            {
                if (isAutoType || isUserType) 
                    property.ShouldSerialize = instance => false;

                if (AlwaysUseNativeFormatting)
                    property.PropertyName = property.UnderlyingName.Decapitalize();

                return property;
            }

            property.ShouldSerialize = instance => true;

            var prop = member as PropertyInfo;
            if (prop != null && prop.PropertyType.IsCollection() && !attr.Nest)
            {
                property.PropertyType = typeof(Guid[]);
                property.ValueProvider = new ItemKeyProvider(prop);
            }

            property.PropertyName = attr.PropertyName ?? property.UnderlyingName.Decapitalize();

            return property;
        }

    }
}
