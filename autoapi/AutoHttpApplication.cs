using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Dispatcher;
using System.Web.Mvc;
using System.Web.Routing;
using Castle.MicroKernel;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Microsoft.AspNet.SignalR;
using Owin;
using zeco.autoapi.DependencyInjection;
using zeco.autoapi.Extensions;
using zeco.autoapi.MVC.Fitlers;
using HttpGetAttribute = System.Web.Mvc.HttpGetAttribute;
using HttpPatchAttribute = System.Web.Mvc.HttpPatchAttribute;
using HttpPostAttribute = System.Web.Mvc.HttpPostAttribute;
using HttpPutAttribute = System.Web.Mvc.HttpPutAttribute;

namespace zeco.autoapi
{

    public abstract class AutoHttpApplication : HttpApplication
    {

        private const string 
            ControllerClassNameSuffix = "Controller",
            ControllerDefaultActionName = "Index";

        internal static class Instance
        {
            public static readonly WindsorInstaller Installer = new WindsorInstaller();
            public static readonly HashSet<Type> RoutedControllers = new HashSet<Type>();
            public static InjectingControllerFactoryBase Factory;
            public static bool IsDefaultActionSet;
        }

        private void AddRoute<TController>(string action, string url = null, bool idparam = false)
            where TController : Controller
        {
            if (url == string.Empty)
            {
                if (Instance.IsDefaultActionSet)
                {
                    const string error = "More than one default action in configuration.";
                    throw new DuplicateNameException(error);
                }
                Instance.IsDefaultActionSet = true;
            }

            var controllerType = typeof (TController);
            var controllerName = controllerType.Name;

            Instance.RoutedControllers.Add(controllerType);

            var controller = GetControllerName<TController>();

            var routeName = string.Format("{0}+{1}+{2}", controllerName, action, url);
            var parameters = idparam ? new {controller, action, id = UrlParameter.Optional} : (object) new {controller, action};


            RouteTable.Routes.MapRoute(routeName, url ?? CreateActionUrl(action, controller, idparam), parameters, new {url = @"^(?!api\/).*"});
        }

        private void AssertActionIsValid<TController>(string name)
        {
            var controllerType = typeof (TController);

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
                var methods = new[]
                {
                    typeof (HttpGetAttribute),
                    typeof (HttpPostAttribute),
                    typeof (HttpPutAttribute),
                    typeof (HttpPatchAttribute)
                };

                var array = matchingActions.Select(a =>
                {
                    var attrs = a.GetCustomAttributes().Select(aa => aa.GetType()).Intersect(methods).ToArray();

                    if (attrs.Length == 0)
                        attrs = new[] {methods[0]};

                    if (attrs.Length > 1)
                        throw new NotSupportedException("More than one HTTP method is is defined");

                    return attrs[0];
                }).ToArray();

                if (array.Intersect(array).Count() != array.Length)
                {
                    const string fmt = "The action '{0}' is not unique on '{1}'.";
                    var error = string.Format(fmt, name, ControllerClassNameSuffix);
                    throw new NotSupportedException(error);
                }
            }

            foreach (var action in matchingActions)
            {
                var rt = action.ReturnType;

                if (rt.IsGenericType && rt.GetGenericTypeDefinition() == typeof (Task<>))
                    rt = rt.GetGenericArguments().Single();

                if (!typeof (ActionResult).IsAssignableFrom(rt))
                {
                    const string fmt = "The action '{0}' does not return an ActionResult.";
                    var error = string.Format(fmt, name);
                    throw new NotSupportedException(error);
                }
            }
        }

        private string CreateActionUrl(string action, string path, bool idparam)
        {
            if (action == ControllerDefaultActionName)
                action = string.Empty;
            if (path == string.Empty)
                return action;

            return string.Format("{0}/{1}{2}", path, action, idparam ? "/{id}" : "");
        }

        private string GetActionName<TController>(LambdaExpression accessor)
        {
            var name = ObjectExtensions.NameOf(accessor);
            AssertActionIsValid<TController>(name);
            return name;
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

        private string GetControllerName<TController>() where TController : Controller
        {
            var type = typeof (TController);
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

        private void SetupApplicationInternal(HttpConfiguration configuration)
        {
            GlobalHost.Configuration.ConnectionTimeout = TimeSpan.FromMinutes(10);
            GlobalHost.Configuration.DisconnectTimeout = TimeSpan.FromSeconds(30);
            GlobalHost.Configuration.KeepAlive = TimeSpan.FromSeconds(10);            

            SetupComponents(configuration);

            Start(configuration);

            Instance.Installer.AddInstaller(SetupInternalControllerInjector);

            SetupCustomControllerFactory(configuration);
        }

        private void SetupCustomControllerFactory(HttpConfiguration conf)
        {
            Instance.Factory = InitializeControllerFactory(Instance.Installer);
            conf.Services.Replace(typeof (IAssembliesResolver), new DefaultAssembliesResolver());
            conf.Services.Replace(typeof(IHttpControllerActivator), Instance.Factory);
            ControllerBuilder.Current.SetControllerFactory(Instance.Factory);
        }

        private void SetupFilters(HttpConfiguration configuration)
        {
            if (this.IsDebug())
            {
                configuration.Filters.Add(new DebugExceptionHandlerAttribute());
            }
        }

        private void SetupHeaders()
        {
            MvcHandler.DisableMvcResponseHeader = true;
        }

        private void SetupRoutes(HttpConfiguration configuration)
        {
            RouteTable.Routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            configuration.MapHttpAttributeRoutes();
        }

        public abstract void ConfigureInjector(IWindsorContainer container, IConfigurationStore store);

        internal virtual InjectingControllerFactoryBase InitializeControllerFactory(WindsorInstaller installer)
        {
            return new InjectingControllerFactoryBase().Install(installer);
        }

        internal virtual void SetupComponents(HttpConfiguration configuration)
        {
            SetupHeaders();
            SetupFilters(configuration);
            SetupRoutes(configuration);
        }

        protected internal virtual void SetupInternalControllerInjector(IWindsorContainer container, IConfigurationStore store)
        {
            foreach (var controller in Instance.RoutedControllers)
                container.Register(Component.For(controller).ImplementedBy(controller).LifestylePerWebRequest());
            ConfigureInjector(container, store);
        }

        protected virtual void ConfigureOwin(IAppBuilder app)
        {
            
        }

        protected abstract void Start(HttpConfiguration configuration);

        internal T Resolve<T>()
        {
            return Instance.Factory.Kernel.Resolve<T>();
        }

        internal object Resolve(string typename)
        {
            var type = Type.GetType(typename);
            return Instance.Factory.Kernel.Resolve(type);
        }

        protected void AddGlobalRoutes<TController>()
            where TController : Controller
        {
            AddRoutes<TController>(string.Empty);
        }

        protected void AddGlobalRoutes<TController>(Expression<Action<TController>> accessor)
            where TController : Controller
        {
            AddRoutes<TController>(string.Empty);
            AddSPARoute(accessor);
        }

        protected void AddRoutes<TController>(string url = null)
            where TController : Controller
        {
            url = url ?? GetControllerName<TController>();

            var actions = GetActionNames<TController>();
            foreach (var action in actions)
            {
                AssertActionIsValid<TController>(action);
                var actionUrl = CreateActionUrl(action, url, false);
                AddRoute<TController>(action, actionUrl);
            }
        }

        protected void AddRoute<TController>(Expression<Action<TController>> accessor, string url = null, bool idparam = false)
            where TController : Controller
        {
            var action = GetActionName<TController>(accessor);
            AddRoute<TController>(action, url, idparam);
        }

        protected void AddSPARoute<TController>(Expression<Action<TController>> accessor)
            where TController : Controller
        {
            var action = GetActionName<TController>(accessor);
            AddRoute<TController>(action, "{*url}");
        }

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
        
        protected void Application_EndRequest(object sender, EventArgs e)
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
    }

    class CastleWindsorDependencyResolver : DefaultDependencyResolver
    {
        public CastleWindsorDependencyResolver(IKernel kernel)
        {
            throw new NotImplementedException();
        }
    }
}