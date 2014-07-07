using System;
using System.Net.Http;
using System.Reflection;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dispatcher;
using System.Web.Mvc;
using System.Web.Routing;
using Castle.MicroKernel;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using zeco.autoapi.CodeGeneration;
using zeco.autoapi.Providers;

namespace zeco.autoapi.DependencyInjection
{
    internal class InjectingControllerFactory<TContext, TUser> : 
        DefaultControllerFactory, IHttpControllerActivator, IDisposable, IWindsorInstaller
        where TUser : AutoApiUser 
        where TContext : AutoApiDbContext<TUser> 
    {
        #region ReleaseWrapper - Inner Class

        class ReleaseWrapper : IDisposable
        {
            #region Fields

            readonly Action _release;

            #endregion

            #region Constructors

            public ReleaseWrapper(Action release)
            {
                _release = release;
            }

            #endregion

            #region Public Methods

            public void Dispose()
            {
                _release();
            }

            #endregion
        }

        #endregion

        #region Fields

        readonly WindsorContainer _container;
        private readonly Assembly _autoApiAssembly;

        #endregion

        #region Public Properties

        public IKernel Kernel
        {
            get { return _container.Kernel; }
        }

        #endregion

        #region Constructors

        public InjectingControllerFactory(Type controllerBase, params IWindsorInstaller[] installers)
        {
            _autoApiAssembly = new AutoApiBuilder(controllerBase).GenerateAutoApiAssembly();
            _container = new WindsorContainer();
            _container.Install(this);
            foreach (var installer in installers)
                _container.Install(installer);
        }

        #endregion

        #region Public Methods

        public IHttpController Create(HttpRequestMessage request, HttpControllerDescriptor controllerDescriptor, Type controllerType)
        {
            var controller = (IHttpController)_container.Resolve(controllerType);
            request.RegisterForDispose(new ReleaseWrapper(() => _container.Release(controller)));
            return controller;
        }

        public void Dispose()
        {
            _container.Dispose();
        }

        public override void ReleaseController(IController controller)
        {
            Kernel.ReleaseComponent(controller);
        }

        #endregion

        #region Other

        protected override IController GetControllerInstance(RequestContext requestContext, Type controllerType)
        {
            if (controllerType == null)
                return null;

            return (IController)Kernel.Resolve(controllerType);
        }

        #endregion

        public void Install(IWindsorContainer container, IConfigurationStore store)
        {

            container.Register(

                Component.For<IUniqueAccessTokenProvider>().ImplementedBy<SecureUniqueAccessTokenProvider>(),

                Component.For<IGuidProvider>().ImplementedBy<ConcurrentGuidProvider>(),

                Component.For<TContext>().LifestylePerWebRequest(),

                Component.For<AutoApiUserStore<TUser>>()
                    .UsingFactoryMethod(c => new AutoApiUserStore<TUser>(c.Resolve<TContext>()))
                    .LifestylePerWebRequest(),

                Component.For<AutoApiUserManager<TUser>>()
                    .UsingFactoryMethod(c => new AutoApiUserManager<TUser>(c.Resolve<AutoApiUserStore<TUser>>()))
                    .LifestylePerWebRequest(),

                Component.For<AutoApiRoleStore>()
                    .UsingFactoryMethod(c => new AutoApiRoleStore(c.Resolve<TContext>()))
                    .LifestylePerWebRequest(),

                Component.For<AutoApiRoleManager>()
                    .UsingFactoryMethod(c => new AutoApiRoleManager(c.Resolve<AutoApiRoleStore>()))
                    .LifestylePerWebRequest(),

                Classes.FromAssembly(_autoApiAssembly).BasedOn<ApiController>().LifestylePerWebRequest()

                );
        }
    }
}