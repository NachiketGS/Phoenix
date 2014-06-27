using System;

namespace Phoenix.DI
{
	public class ConstructorParam
	{
		public string Param { get; private set; }
		public Object Object { get; private set; }

		public ConstructorParam(string param, Object obj)
		{
			this.Object = obj;
			this.Param = param;
		}
	}
}
