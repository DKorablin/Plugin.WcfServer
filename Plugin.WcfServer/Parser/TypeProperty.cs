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
			IsArray = type.IsArray;
			IsCollection = false;
			IsGeneric = type.IsGeneric;
			IsDictionary = false;
			IsKeyValuePair = false;
			IsNullable = false;
			IsStruct = type.IsValueType;
		}

		public TypeProperty(Type type)
		{
			IsArray = type.IsArray;
			IsCollection = false;
			IsGeneric = type.IsGenericType;
			IsDictionary = false;
			IsKeyValuePair = false;
			IsNullable = false;
			IsStruct = type.IsValueType;
		}
	}
}