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

        internal readonly Assembly AutoApiAssembly;

        internal InjectingControllerFactory(Type controllerBase)
        {
            AutoApiAssembly = new AutoApiBuilder(controllerBase).GenerateAutoApiAssembly();
            Util.RegisterAutoApiAssembly(AutoApiAssembly);
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

            if (AutoApiAssembly != null)
                _container.Register(Classes.FromAssembly(AutoApiAssembly).BasedOn<ApiController>().LifestylePerWebRequest());

            return this;
        }
    }
}