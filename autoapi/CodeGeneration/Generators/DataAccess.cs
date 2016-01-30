using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace zeco.autoapi.CodeGeneration.Generators
{
    class DataAccess : TypeScriptCodeGenerator
    {
        public override string Filename => "data.access.ts";

        protected override void GenerateInternal()
        {
            Scope($"namespace {ModuleName}", () =>
            {
                var enums = new HashSet<Type>();

                foreach (var type in GetDatatypes())
                {
                    foreach (var property in type.GetProperties().OrderBy(o => o.Name))
                    {
                        var attr = property.GetCustomAttribute<AutoPropertyAttribute>();
                        if (attr != null)
                        {
                            var ptype = property.PropertyType;
                            if (ptype.IsEnum) enums.Add(ptype);
                        }
                    }
                }

                foreach (var @enum in enums)
                {
                    Scope($"export enum {@enum.Name}", () =>
                    {
                        var values = @enum.GetEnumValues();
                        var names = @enum.GetEnumNames();
                        for (var i = 0; i < names.Length; i++)
                            Statement($"{names[i]} = {(int) values.GetValue(i)}{","}");
                    });

                }

                Scope("export interface IDataService", () =>
                {

                    foreach (var type in GetDatatypes())
                        Statement($"{GetPluralName(type)}: ICommunicator<{GetInterfaceName(type)}>;");

                    Statement("clear: () => void;");
                    Statement("self: (id?:string) => ng.IPromise<IUser>;");
                });

                Scope("export namespace factories", () =>
                {
                    Scope("export function data(entityService:IEntityService, init:IInitializationService) : IDataService", () =>
                    {

                        Const("cache", "init.cache");
                        foreach (var type in GetDatatypes())
                        {
                            Const(GetPluralName(type),
                                $"new entityService.communicator<{GetInterfaceName(type)}>('{Route(type)}', '{type.FullName}', cache)");
                        }

                        Const("self", "(id = null) => users.get(id || init.global.userId)");

                        var dict = new Dictionary<string, Action>();

                        foreach (var type in GetDatatypes())
                            dict.Add(GetPluralName(type), () => Statement(GetPluralName(type)));

                        dict.Add("clear", () => Statement("entityService.clear"));
                        dict.Add("self", () => Statement("self"));
                        JObject("service", dict);

                        Return("service");
                    });

                    Statement("data.$inject=[\"entityService\", \"initialization\"];");
                });
            });
        }

    }
}