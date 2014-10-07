using System;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dispatcher;
using System.Web.Mvc;
using System.Web.Routing;
using Castle.MicroKernel;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using zeco.autoapi.Providers;

namespace zeco.autoapi.DependencyInjection
{
    internal class InjectingControllerFactoryBase : DefaultControllerFactory, IDisposable, IHttpControllerActivator
    {

        protected readonly WindsorContainer _container;

        public IKernel Kernel
        {
            get { return _container.Kernel; }
        }

        public IHttpController Create(HttpRequestMessage request, HttpControllerDescriptor controllerDescriptor, Type controllerType)
        {
            return (IHttpController)_container.Resolve(controllerType);
        }

        protected override IController GetControllerInstance(RequestContext requestContext, Type controllerType)
        {
            if (controllerType == null)
                return null;
            return (IController) Kernel.Resolve(controllerType);
        }

        public void Dispose()
        {
            _container.Dispose();
        }

        public override void ReleaseController(IController controller)
        {
            Kernel.ReleaseComponent(controller);
        }

        public InjectingControllerFactoryBase()
        {
            _container = new WindsorContainer();
        }

        public virtual InjectingControllerFactoryBase Install(params WindsorInstaller[] installers)
        {

            _container.Register(

                Component.For<IUniqueAccessTokenProvider>().ImplementedBy<SecureUniqueAccessTokenProvider>(),

                Component.For<IGuidProvider>().ImplementedBy<ConcurrentGuidProvider>()

                );

            foreach (var installer in installers)
                _container.Install(installer);

            return this;
        }
    }
}