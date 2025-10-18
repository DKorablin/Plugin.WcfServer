using System;
using System.Collections.Generic;
using System.Reflection;
#if NET35
using System.Web.Script.Serialization;
#else
using System.Text.Json;
#endif

namespace Plugin.WcfServer
{
	/// <summary>Serialization</summary>
	internal static class Serializer
	{
		/// <summary>Deserialize JSON string to object</summary>
		/// <typeparam name="T">Object type</typeparam>
		/// <param name="json">JSON formatted string</param>
		/// <returns>Deserialized object</returns>
		public static Dictionary<String, Object> JavaScriptDeserialize(String json)
		{
			if(String.IsNullOrEmpty(json))
				return new Dictionary<String, Object>();

#if NET35
			JavaScriptSerializer serializer = new JavaScriptSerializer();
			return (Dictionary<String, Object>)serializer.DeserializeObject(json);
#else
			return JsonSerializer.Deserialize<Dictionary<String, Object>>(json);
#endif
		}

		/// <summary>Deserialize JSON string to object</summary>
		/// <typeparam name="T">Object type</typeparam>
		/// <param name="json">JSON formatted string</param>
		/// <returns>Deserialized object</returns>
		public static T JavaScriptDeserialize<T>(String json)
		{
			if(String.IsNullOrEmpty(json))
				return default;

#if NET35
			JavaScriptSerializer serializer = new JavaScriptSerializer();
			return serializer.Deserialize<T>(json);
#else
			return JsonSerializer.Deserialize<T>(json);
#endif
		}

		/// <summary>Deserialize JSON string to object</summary>
		/// <typeparam name="T">Object type</typeparam>
		/// <param name="json">JSON formatted string</param>
		/// <returns>Deserialized object</returns>
		public static Object JavaScriptDeserialize(Type type, String json)
		{
			if(String.IsNullOrEmpty(json))
				return null;

#if NET35
			JavaScriptSerializer serializer = new JavaScriptSerializer();
			return serializer.GetType().InvokeMember("Deserialize", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, null, new Object[] { serializer, json, type, serializer.RecursionLimit });
#else
			return JsonSerializer.Deserialize(json, type);
#endif
		}

		/// <summary>Serialize object</summary>
		/// <param name="item">Object to serialize</param>
		/// <returns>JSON formatted string</returns>
		public static String JavaScriptSerialize(Object item)
		{
			if(item == null)
				return null;

#if NET35
			JavaScriptSerializer serializer = new JavaScriptSerializer();
			return serializer.Serialize(item);
#else
			return JsonSerializer.Serialize(item);
#endif
		}
	}
}
