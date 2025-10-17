using System;
using System.Collections.Generic;
using System.Reflection;
using System.Web.Script.Serialization;

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

			JavaScriptSerializer serializer = new JavaScriptSerializer();
			return (Dictionary<String, Object>)serializer.DeserializeObject(json);
		}

		/// <summary>Deserialize JSON string to object</summary>
		/// <typeparam name="T">Object type</typeparam>
		/// <param name="json">JSON formatted string</param>
		/// <returns>Deserialized object</returns>
		public static T JavaScriptDeserialize<T>(String json)
		{
			if(String.IsNullOrEmpty(json))
				return default;

			JavaScriptSerializer serializer = new JavaScriptSerializer();
			return serializer.Deserialize<T>(json);
		}

		/// <summary>Deserialize JSON string to object</summary>
		/// <typeparam name="T">Object type</typeparam>
		/// <param name="json">JSON formatted string</param>
		/// <returns>Deserialized object</returns>
		public static Object JavaScriptDeserialize(Type type, String json)
		{
			if(String.IsNullOrEmpty(json))
				return null;

			JavaScriptSerializer serializer = new JavaScriptSerializer();
			return serializer.GetType().InvokeMember("Deserialize", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, null, new Object[] { serializer, json, type, serializer.RecursionLimit });
		}

		/// <summary>Serialize object</summary>
		/// <param name="item">Object to serialize</param>
		/// <returns>JSON formatted string</returns>
		public static String JavaScriptSerialize(Object item)
		{
			if(item == null)
				return null;

			JavaScriptSerializer serializer = new JavaScriptSerializer();
			return serializer.Serialize(item);
		}
	}
}