using System;


namespace Phoenix.DI
{
	[System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct,
							AllowMultiple = false)]
	public class Export : System.Attribute
	{
		private Type _contractType { get; set; }
		public Type ContractType
		{
			get
			{
				return _contractType;
			}
		}

		private CreationPolicy _creationPolicy { get; set; }

		public CreationPolicy CreationPolicy
		{
			get
			{
				return _creationPolicy;
			}
		}

		public Export(Type type, CreationPolicy creationPolicy)
		{
			this._creationPolicy = creationPolicy;
			this._contractType = type;
		}

		public Export(Type type)
		{
			this._contractType = type;
			this._creationPolicy = CreationPolicy.Shared;
		}
	}

	public enum CreationPolicy
	{
		Shared,
		NonShared
	}
}
