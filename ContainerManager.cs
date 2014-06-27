using System;
using System.Reflection;

namespace Phoenix.DI
{
    public static class ContainerManager
    {
        public static IPhoenixContainer Default { get; private set; }

        static ContainerManager()
        {
            var container = new PhoenixContainer();

            container.RegisterAssembly(Assembly.GetExecutingAssembly());

            Default = container;
        }

        public static IPhoenixContainer CreateContainer()
        {
            return new PhoenixContainer();
        }
    }
}
