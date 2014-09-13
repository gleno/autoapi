using System;
using System.Collections.Generic;

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
                Scope("export interface IDataService", () =>
                {

                    foreach (var type in GetDatatypes())
                        Statement(string.Format("{0}: ICommunicator<{1}>;", 
                            GetPluralName(type), GetInterfaceName(type)));

                    Statement(string.Format("clear: () => void;"));
                    Statement(string.Format("self: () => ng.IPromise<IUser>"));
                });

                Scope("export module factories", () =>
                {

                    Function("load", () =>
                    {
                        Var("element", "document.getElementById(container)");
                        If("element != null", () => Return("JSON.parse(element.innerHTML)"));
                        Return("null");
                    }, "container");

                    Scope("export function data(entityService:IEntityService) : IDataService", () =>
                    {

                        Var("global", "load('__global')");
                        Var("cache", "load('__cache') || {}");


                        foreach (var type in GetDatatypes())
                        {
                            Var(GetPluralName(type), string.Format("new entityService.communicator<{0}>('{1}', {2})", GetInterfaceName(type), Route(type), "(<any>cache['" + type.FullName + "'] || {})"));
                        }

                        Var("self", "() => users.get(global.userId); ");

                        var dict = new Dictionary<string, Action>();

                        foreach (var type in GetDatatypes())
                            dict.Add(GetPluralName(type), () => Statement(GetPluralName(type)));

                        dict.Add("clear", () => Statement("entityService.clear"));
                        dict.Add("self", () => Statement("self"));
                        JObject("service", dict);

                        Return("service");
                    });

                    Statement("data.$inject=['entityService'];");
                });
            });
        }

    }
}