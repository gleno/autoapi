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