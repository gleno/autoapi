using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Dispatcher;
using System.Web.Mvc;
using System.Web.Routing;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using zeco.autoapi.CodeGeneration;
using zeco.autoapi.DependencyInjection;
using zeco.autoapi.Extensions;
using zeco.autoapi.Json;
using zeco.autoapi.MVC.Fitlers;

namespace zeco.autoapi
{
    public abstract class AutoApiHttpApplication<TContext, TUser, TBaseController> : HttpApplication
        where TBaseController : AutoApiController<TUser, TContext, TUser> 
        where TUser : AutoApiUser, new() where TContext : AutoApiDbContext<TUser>
    {
        
        #region Fields

        private const string ControllerClassNameSuffix = "Controller";
        private const string ControllerDefaultActionName = "Index";

        private bool _isDefaultActionSet;
        private readonly HashSet<Type> _routedControllers = new HashSet<Type>();
        private readonly WindsorInstaller _installer = new WindsorInstaller();

        #endregion

        protected void AddGlobalRoutes<TController>()
            where TController : Controller
        {
            AddRoutes<TController>(string.Empty);
        }

        protected void AddRoutes<TController>(string path = null)
            where TController : Controller
        {
            path = path ?? GetControllerName<TController>();

            var actions = GetActionNames<TController>();
            foreach (var action in actions)
            {
                AssertActionIsValid<TController>(action);
                var url = CreateActionUrl(action, path);
                AddRoute<TController>(action, url);
            }
        }

        protected void AddRoute<TController>(Expression<Action<TController>> accessor, string url = null)
            where TController : Controller
        {
            var action = GetActionName<TController>(accessor);
            AddRoute<TController>(action);
        }

        protected abstract void Start(HttpConfiguration configuration);

        protected abstract void SetupControllerInjector(IWindsorContainer container, IConfigurationStore store);

        #region IIS API Surface

        protected void Application_Start(object sender, EventArgs e)
        {
            GlobalConfiguration.Configure(SetupApplicationInternal);
        }

        protected void Session_Start(object sender, EventArgs e)
        {

        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {

        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {

        }

        protected void Application_Error(object sender, EventArgs e)
        {

        }

        protected void Session_End(object sender, EventArgs e)
        {

        }

        protected void Application_End(object sender, EventArgs e)
        {

        }

        #endregion

        #region Helpers



        private void SetupSomeCastleWindsor(IWindsorContainer container, IConfigurationStore store)
        {
            foreach (var controller in _routedControllers)
                container.Register(Component.For(controller).ImplementedBy(controller).LifestylePerWebRequest());
        }

        private string GetControllerName<TController>() where TController : Controller
        {
            var type = typeof(TController);
            var name = type.Name;
            var fullName = type.FullName;

            if (!name.EndsWith(ControllerClassNameSuffix))
            {
                const string fmt = "The controller {0} has a name that doesn't end with '{1}'.";
                var error = string.Format(fmt, fullName, ControllerClassNameSuffix);
                throw new NotSupportedException(error);
            }

            if (!type.IsUniqueByName())
            {
                const string fmt = "The type {0} is not unique across the current AppDomain.";
                var error = string.Format(fmt, type.Name);
                throw new DuplicateNameException(error);
            }

            return name.Substring(0, name.Length - ControllerClassNameSuffix.Length);
        }

        private string GetActionName<TController>(LambdaExpression accessor)
        {
            var name = ObjectExtensions.NameOf(accessor);
            AssertActionIsValid<TController>(name);
            return name;
        }

        private void AssertActionIsValid<TController>(string name)
        {
            var controllerType = typeof(TController);

            var matchingActions = controllerType
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(mi => mi.Name == name)
                .ToArray();

            var matchingActionCount = matchingActions.Length;

            if (matchingActionCount == 0)
            {
                const string fmt = "The action '{0}' does not exist on '{1}'.";
                var error = string.Format(fmt, name, ControllerClassNameSuffix);
                throw new NotSupportedException(error);
            }

            if (matchingActionCount > 1)
            {
                const string fmt = "The action '{0}' is not unique on '{1}'.";
                var error = string.Format(fmt, name, ControllerClassNameSuffix);
                throw new NotSupportedException(error);
            }

            if (matchingActionCount == 1)
            {
                var matchingAction = matchingActions.Single();
                var rt = matchingAction.ReturnType;

                if (rt.IsGenericType && rt.GetGenericTypeDefinition() == typeof(Task<>))
                    rt = rt.GetGenericArguments().Single();

                if (!typeof(ActionResult).IsAssignableFrom(rt))
                {
                    const string fmt = "The action '{0}' does not return an ActionResult.";
                    var error = string.Format(fmt, name);
                    throw new NotSupportedException(error);
                }
            }
        }

        private void SetupApiFormatters(HttpConfiguration conf)
        {
            var formatters = conf.Formatters;

            //Remove XML formatter so that WEB API isn't being too clever
            formatters.Remove(GlobalConfiguration.Configuration.Formatters.XmlFormatter);
            formatters.JsonFormatter.SerializerSettings.ContractResolver = new JsonContractResolver();
        }

        private void SetupCustomControllerFactory(HttpConfiguration conf)
        {

            var factory = new InjectingControllerFactory<TContext, TUser>(typeof(TBaseController), _installer);

            conf.Services.Replace(typeof (IAssembliesResolver), new DefaultAssembliesResolver());
            conf.Services.Replace(typeof (IHttpControllerActivator), factory);
            ControllerBuilder.Current.SetControllerFactory(factory);
        }

        private void SetupApplicationInternal(HttpConfiguration configuration)
        {
            SetupFilters(configuration);
            SetupApiFormatters(configuration);
            SetupIgnoreRoutes();
            SetupAutoApiRoutes(configuration);

            Start(configuration);

            _installer.AddInstaller(SetupSomeCastleWindsor);
            _installer.AddInstaller(SetupControllerInjector);

            SetupCustomControllerFactory(configuration);
        }

        private void SetupFilters(HttpConfiguration configuration)
        {
            if (this.IsDebug())
            {
                configuration.Filters.Add(new DebugExceptionHandlerAttribute());
            }
        }

        private void SetupIgnoreRoutes()
        {
            RouteTable.Routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
        }

        private void SetupAutoApiRoutes(HttpConfiguration configuration)
        {
            configuration.MapHttpAttributeRoutes();
            configuration.Routes.MapHttpRoute("AutoAPI", "api/{controller}/{id}", new {id = RouteParameter.Optional});
        }

        private IEnumerable<string> GetActionNames<TController>()
            where TController : Controller
        {
            var allMethods = typeof (TController)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(method => !method.IsSpecialName)
                .Select(method => method.Name)
                .ToArray();

            var controllerPublicMethods = typeof (Controller)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Select(method => method.Name)
                .ToArray();

            return allMethods.Except(controllerPublicMethods).ToArray();
        }

        private void AddRoute<TController>(string action, string url = null)
            where TController : Controller
        {
            if (url == string.Empty)
            {
                if (_isDefaultActionSet)
                {
                    const string error = "More than one default action in configuration.";
                    throw new DuplicateNameException(error);
                }
                _isDefaultActionSet = true;
            }

            var controllerType = typeof (TController);
            var controllerName = controllerType.Name;

            _routedControllers.Add(controllerType);

            var controller = GetControllerName<TController>();

            var routeName = string.Format("{0}+{1}", controllerName, action);
            var parameters = new {controller, action};
            RouteTable.Routes.MapRoute(routeName, url ?? CreateActionUrl(action, controller), parameters);
        }

        private string CreateActionUrl(string action, string path)
        {
            if (action == ControllerDefaultActionName)
                action = string.Empty;
            if (path == string.Empty)
                return action;

            return string.Format("{0}/{1}", path, action);
        }

        #endregion

    }
}