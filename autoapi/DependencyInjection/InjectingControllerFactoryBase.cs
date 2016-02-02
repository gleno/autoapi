using System;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dispatcher;
using System.Web.Mvc;
using System.Web.Routing;
using autoapi.Providers;
using Castle.MicroKernel;
using Castle.MicroKernel.Registration;
using Castle.Windsor;

namespace autoapi.DependencyInjection
{
    internal class InjectingControllerFactoryBase : DefaultControllerFactory, IDisposable, IHttpControllerActivator
    {

        protected readonly WindsorContainer Container;

        public IKernel Kernel => Container.Kernel;

        public IHttpController Create(HttpRequestMessage request, HttpControllerDescriptor controllerDescriptor, Type controllerType)
        {
            return (IHttpController)Container.Resolve(controllerType);
        }

        protected override IController GetControllerInstance(RequestContext requestContext, Type controllerType)
        {
            if (controllerType == null)
                return null;
            return (IController) Kernel.Resolve(controllerType);
        }

        public void Dispose()
        {
            Container.Dispose();
        }

        public override void ReleaseController(IController controller)
        {
            Kernel.ReleaseComponent(controller);
        }

        public InjectingControllerFactoryBase()
        {
            Container = new WindsorContainer();
        }

        public virtual InjectingControllerFactoryBase Install(params WindsorInstaller[] installers)
        {

            Container.Register(

                Component.For<IUniqueAccessTokenProvider>().ImplementedBy<SecureUniqueAccessTokenProvider>(),

                Component.For<IGuidProvider>().ImplementedBy<ConcurrentGuidProvider>()

                );

            foreach (var installer in installers)
                Container.Install(installer);

            return this;
        }
    }
}