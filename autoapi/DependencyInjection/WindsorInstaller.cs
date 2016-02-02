using System.Collections.Generic;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;

namespace autoapi.DependencyInjection
{
    internal class WindsorInstaller : IWindsorInstaller
    {
        public delegate void WindsorInstallerDelegate(IWindsorContainer container, IConfigurationStore store);

        private readonly List<WindsorInstallerDelegate> _installers = new List<WindsorInstallerDelegate>();

        public void AddInstaller(WindsorInstallerDelegate installer)
        {
            _installers.Add(installer);
        }

        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            foreach (var installer in _installers)
                installer(container, store);
        }
    }
}