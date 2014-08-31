using System.Web.Http;
using zeco.autoapi.DependencyInjection;
using zeco.autoapi.Json;

namespace zeco.autoapi
{
    public abstract class AutoApiHttpApplication<TContext, TUser, TBaseController> : AutoHttpApplication 
        where TUser : AutoApiUser, new() where TContext : AutoApiDbContext<TUser>
    {
        protected override void SetupComponents(HttpConfiguration configuration)
        {
            base.SetupComponents(configuration);

            SetupApiFormatters(configuration);
            SetupAutoApiRoutes(configuration);
        }

        private void SetupApiFormatters(HttpConfiguration conf)
        {
            var formatters = conf.Formatters;

            //Remove XML formatter so that WEB API isn't being too clever
            formatters.Remove(GlobalConfiguration.Configuration.Formatters.XmlFormatter);
            formatters.JsonFormatter.SerializerSettings.ContractResolver = new JsonContractResolver();
        }

        private void SetupAutoApiRoutes(HttpConfiguration configuration)
        {
            configuration.MapHttpAttributeRoutes();
            configuration.Routes.MapHttpRoute("AutoAPI", "api/{controller}/{id}", new {id = RouteParameter.Optional});
        }

        internal override InjectingControllerFactoryBase GetControllerFactoryBase(WindsorInstaller installer)
        {
            return new InjectingControllerFactory<TContext, TUser>(typeof (TBaseController)).Install(installer);
        }
    }
}