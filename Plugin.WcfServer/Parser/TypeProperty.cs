using System;
using SAL.Flatbed;

namespace Plugin.WcfServer.Parser
{
	internal class TypeProperty
	{
		public Boolean IsArray { get; set; }
		public Boolean IsCollection { get; set; }
		public Boolean IsGeneric { get; set; }
		public Boolean IsDictionary { get; set; }
		public Boolean IsKeyValuePair { get; set; }
		public Boolean IsNullable { get; set; }
		public Boolean IsStruct { get; set; }

		public TypeProperty(IPluginTypeInfo type)
		{
			this.IsArray = type.IsArray;
			this.IsCollection = false;
			this.IsGeneric = type.IsGeneric;
			this.IsDictionary = false;
			this.IsKeyValuePair = false;
			this.IsNullable = false;
			this.IsStruct = type.IsValueType;
		}

		public TypeProperty(Type type)
		{
			this.IsArray = type.IsArray;
			this.IsCollection = false;
			this.IsGeneric = type.IsGenericType;
			this.IsDictionary = false;
			this.IsKeyValuePair = false;
			this.IsNullable = false;
			this.IsStruct = type.IsValueType;
		}
	}
}