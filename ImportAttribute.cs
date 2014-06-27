using System;

namespace Phoenix.DI
{
	[System.AttributeUsage(System.AttributeTargets.Property | System.AttributeTargets.Field,
							AllowMultiple = false)]
	public class Import : System.Attribute
	{

	}
}
