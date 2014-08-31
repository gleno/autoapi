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
using System.Web.UI;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using zeco.autoapi.DependencyInjection;
using zeco.autoapi.Extensions;
using zeco.autoapi.MVC.Fitlers;

namespace zeco.autoapi
{
    public abstract class AutoHttpApplication : HttpApplication 
    {

        private bool _isDefaultActionSet;
        private readonly HashSet<Type> _routedControllers = new HashSet<Type>();

        internal readonly WindsorInstaller _installer = new WindsorInstaller();

        private const string ControllerClassNameSuffix = "Controller";
        private const string ControllerDefaultActionName = "Index";

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

        protected internal virtual void SetupInternalControllerInjector(IWindsorContainer container, IConfigurationStore store)
        {
            foreach (var controller in _routedControllers)
                container.Register(Component.For(controller).ImplementedBy(controller).LifestylePerWebRequest());
            ConfigureInjector(container, store);
        }

        public abstract void ConfigureInjector(IWindsorContainer container, IConfigurationStore store);

        protected void Application_Start(object sender, EventArgs e)
        {
            GlobalConfiguration.Configure(SetupApplicationInternal);
        }

        protected void Session_Start(object sender, EventArgs e)
        {

        }

        class W : StringWriter
        {
            private readonly TextWriter _writer;

            public W(TextWriter writer)
            {
                _writer = writer;
            }

            public override void Close()
            {
                var content = ToString();
                _writer.Write(content);
                _writer.Close();
            }
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

        internal virtual InjectingControllerFactoryBase GetControllerFactoryBase(WindsorInstaller installer)
        {
            return new InjectingControllerFactoryBase().Install(installer);
        }

        private void SetupCustomControllerFactory(HttpConfiguration conf)
        {

            var factory = GetControllerFactoryBase(_installer);

            conf.Services.Replace(typeof (IAssembliesResolver), new DefaultAssembliesResolver());
            conf.Services.Replace(typeof (IHttpControllerActivator), factory);
            ControllerBuilder.Current.SetControllerFactory(factory);
        }

        protected virtual void SetupComponents(HttpConfiguration configuration)
        {
            SetupHeaders();
            SetupFilters(configuration);
            SetupIgnoreRoutes();
        }

        private void SetupApplicationInternal(HttpConfiguration configuration)
        {
            SetupComponents(configuration);

            Start(configuration);

            _installer.AddInstaller(SetupInternalControllerInjector);

            SetupCustomControllerFactory(configuration);
        }

        private void SetupHeaders()
        {
            MvcHandler.DisableMvcResponseHeader = true;
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
    }
}