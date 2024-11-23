using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;

namespace Plugin.WcfServer.Extensions
{
	public static class TypeExtension
	{
		internal static Boolean IsDictionaryType(this Type type)
			=> type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>);

		internal static Boolean IsCollectionType(this Type type)
			=> type.ContainsCustomAttribute(typeof(CollectionDataContractAttribute), true);

		public static Boolean ContainsCustomAttribute(this MemberInfo info, Type attributeType, Boolean inherit)
		{
			if(info.Module.Assembly.ReflectionOnly)
			{
				foreach(CustomAttributeData attribute in CustomAttributeData.GetCustomAttributes(info))
					if(attribute.Constructor.DeclaringType.FullName == attributeType.FullName)
						return true;
				return false;
			} else
				return info.GetCustomAttributes(attributeType, inherit).Length > 0;
		}

		/// <summary>This type from Basic Class Library</summary>
		/// <param name="type">Type to check</param>
		/// <returns>Type from BCL</returns>
		public static Boolean IsBclType(this Type type)
		{
			switch(type.Assembly.GetName().Name)
			{
			case "mscorlib":
			case "System.Private.CoreLib":
				return true;
			default:
				return false;
			}
		}
	}
}