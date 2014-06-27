using System;
using System.Reflection;

namespace Phoenix.DI
{
	public interface IPhoenixContainer
	{
		void RegisterType<TType, TImplementation>(Export exportAttribute = null);

		void RegisterType<TType>(Export expAttribute = null);

		void RegisterAssembly(Assembly assembly);

        void RegisterInstance<TType>(object objInstance);

		T Resolve<T>(ConstructorParam[] paramsList = null);

	}
}
