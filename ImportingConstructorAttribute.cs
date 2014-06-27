using System;

namespace Phoenix.DI
{
	[System.AttributeUsage(System.AttributeTargets.Constructor,
							AllowMultiple = false)]
	public class ImportingConstructor : System.Attribute
	{

	}
}
