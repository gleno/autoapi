using System;
using System.Reflection;
using System.Web.Http;
using System.Web.Mvc;
using Castle.MicroKernel.Registration;
using zeco.autoapi.CodeGeneration;

namespace zeco.autoapi.DependencyInjection
{
    internal class InjectingControllerFactory<TContext, TUser> : InjectingControllerFactoryBase
        where TUser : AutoApiUser 
        where TContext : AutoApiDbContext<TUser> 
    {

        private readonly Assembly _autoApiAssembly;

        public InjectingControllerFactory(Type controllerBase)
        {
            _autoApiAssembly = new AutoApiBuilder(controllerBase).GenerateAutoApiAssembly();
        }

        public override void ReleaseController(IController controller)
        {
            Kernel.ReleaseComponent(controller);
        }

        public override InjectingControllerFactoryBase Install(params WindsorInstaller[] installers)
        {
            base.Install(installers);

            _container.Register(

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
                    .LifestylePerWebRequest()
                );

            if(_autoApiAssembly != null)
                _container.Register(Classes.FromAssembly(_autoApiAssembly).BasedOn<ApiController>().LifestylePerWebRequest());

            return this;
        }
    }
}