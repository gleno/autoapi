using System;
using System.Collections.Generic;

namespace zeco.autoapi.CodeGeneration
{
    class TypeScriptClientInterfaceGenerator : TypeScriptCodeGenerator
    {

        #region Public Properties

        public override string Filename
        {
            get { return "data.access.ts"; }
        }

        #endregion

        #region Public Methods

        protected override void GenerateInternal()
        {
            Scope(string.Format("module {0}", ModuleName), () =>
            {
                Scope("export interface IDataService", () =>
                {

                    foreach (var type1 in GetDatatypes())
                        Statement(string.Format("{0}: ICommunicator<{1}>;", 
                            GetPluralName(type1), GetInterfaceName(type1)));

                    Statement(string.Format("clear: () => void;"));
                    Statement(string.Format("self: () => ng.IPromise<IUser>"));
                });

                Scope("export module factories", () =>
                    Scope("export function data(entityService:IEntityService) : IDataService", () =>
                    {

                        foreach (var type in GetDatatypes())
                            Var(GetPluralName(type), string.Format("new entityService.communicator<{0}>('{1}')", GetInterfaceName(type), Route(type)));


                        Var("self", "() => { " +
                                    "var globdata = document.getElementById('__global').innerHTML;" +
                                    "var global = JSON.parse(globdata);" +
                                    "return users.get(global.userId);" +
                                    "}");

                        var dict = new Dictionary<string, Action>();

                        foreach (var type in GetDatatypes())
                            dict.Add(GetPluralName(type), () => Statement(GetPluralName(type)));

                        dict.Add("clear", () => Statement("entityService.clear"));
                        dict.Add("self", () => Statement("self"));
                        JObject("service", dict);

                        Return("service");
                    }));
            });
        }

        #endregion

    }
}