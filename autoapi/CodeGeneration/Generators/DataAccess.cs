using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using zeco.autoapi.Extensions;

namespace zeco.autoapi.CodeGeneration.Generators
{
    class DataAccess : TypeScriptCodeGenerator
    {
        public override string Filename
        {
            get { return "data.access.ts"; }
        }

        protected override void GenerateInternal()
        {
            Scope(string.Format("module {0}", ModuleName), () =>
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
                    Scope(string.Format("export enum {0}", @enum.Name), () =>
                    {
                        var values = @enum.GetEnumValues();
                        var names = @enum.GetEnumNames();
                        for (var i = 0; i < names.Length; i++)
                            Statement(string.Format("{0} = {1}{2}", names[i], (int)values.GetValue(i), ","));
                    });

                }

                Scope("export interface IDataService", () =>
                {

                    foreach (var type in GetDatatypes())
                        Statement(string.Format("{0}: ICommunicator<{1}>;", 
                            GetPluralName(type), GetInterfaceName(type)));

                    Statement("clear: () => void;");
                    Statement("self: () => ng.IPromise<IUser>");
                });

                Scope("export module factories", () =>
                {
                    Scope("export function data(entityService:IEntityService, init:IInitializationService) : IDataService", () =>
                    {

                        Var("cache", "init.cache");
                        foreach (var type in GetDatatypes())
                        {
                            Var(GetPluralName(type), string.Format("new entityService.communicator<{0}>('{1}', '{2}', cache)", GetInterfaceName(type), Route(type), type.FullName));
                        }

                        Var("self", "() => users.get(init.global.userId); ");

                        var dict = new Dictionary<string, Action>();

                        foreach (var type in GetDatatypes())
                            dict.Add(GetPluralName(type), () => Statement(GetPluralName(type)));

                        dict.Add("clear", () => Statement("entityService.clear"));
                        dict.Add("self", () => Statement("self"));
                        JObject("service", dict);

                        Return("service");
                    });

                    Statement("data.$inject=['entityService', 'initialization'];");
                });
            });
        }

    }
}