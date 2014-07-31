using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using zeco.autoapi.Extensions;

namespace zeco.autoapi.CodeGeneration
{
    class TypeScriptInterfaceGenerator : TypeScriptCodeGenerator
    {
        public override string Filename
        {
            get { return "data.d.ts"; }
        }

        protected override void GenerateInternal()
        {
            Scope(string.Format("declare module {0}", ModuleName), () =>
            {

                var itemprops = new HashSet<string>();

                Scope("export interface IItem",
                    () =>
                    {
                        var type = typeof (Item);
                        foreach (var property in type.GetProperties())
                        {
                            var attr = property.GetCustomAttribute<AutoPropertyAttribute>();
                            if (attr != null)
                            {
                                var name = attr.PropertyName ?? property.Name.Decapitalize();
                                var typename = GetInterfaceName(property.PropertyType);
                                Statement(string.Format("{0}: {1};", name, typename));
                                itemprops.Add(name);
                            }
                        }
                        Statement(string.Format("{0}: {1};", "sourceId?", "string"));

                    });


                var enums = new HashSet<Type>();

                foreach (var type in GetDatatypes())
                {
                    Scope(string.Format("interface I{0} extends IItem", type.Name),
                        () =>
                        {
                            foreach (var property in type.GetProperties().OrderBy(o => o.Name))
                            {
                                var attr = property.GetCustomAttribute<AutoPropertyAttribute>();
                                if (attr != null)
                                {
                                    var name = attr.PropertyName ?? property.Name.Decapitalize();

                                    var ptype = property.PropertyType;

                                    if (itemprops.Contains(name))
                                        continue;

                                    var typename = GetInterfaceName(property.PropertyType);
                                    if (ptype.IsEnum)
                                    {
                                        typename = ptype.Name;
                                        enums.Add(ptype);
                                    }

                                    var nullable = false;

                                    if (!ptype.IsValueType && property.IsAutoProperty() && !ptype.IsCollection())
                                        nullable = property.GetCustomAttribute<RequiredAttribute>() == null;
                                    else if (ptype.IsNullableValueType())
                                        nullable = true;

                                    Statement(string.Format("{0}{1}: {2};", name, nullable ? "?" : "", typename));

                                }
                            }
                        }
                        );
                }

                foreach (var @enum in enums)
                {
                    Scope(string.Format("enum {0}", @enum.Name), () =>
                    {
                        var values = @enum.GetEnumValues();
                        var names = @enum.GetEnumNames();
                        for (int i = 0; i < names.Length; i++)
                        {
                            Statement(string.Format("{0} = {1}{2}", names[i], (int) values.GetValue(i), ","));
                        }
                    });

                }
            });
        }
    }

}