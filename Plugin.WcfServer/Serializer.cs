using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Plugin.WcfServer
{
	/// <summary>
	/// JSON serialization helper based on Newtonsoft.Json.
	/// Provides simple serialization and deserialization methods
	/// equivalent to the legacy JavaScriptSerializer behavior.
	/// </summary>
	internal static class Serializer
	{
		private static readonly JsonSerializerSettings DefaultSettings = new JsonSerializerSettings
		{
			// Match legacy behavior as much as possible
			NullValueHandling = NullValueHandling.Ignore,
			MissingMemberHandling = MissingMemberHandling.Ignore,
			Formatting = Formatting.None
		};

		/// <summary>Deserialize a JSON string into a dictionary.</summary>
		/// <param name="json">JSON-formatted string.</param>
		/// <returns>Deserialized dictionary instance.</returns>
		public static Dictionary<String, Object> JavaScriptDeserialize(String json)
			=> String.IsNullOrEmpty(json)
				? new Dictionary<String, Object>()
				: JsonConvert.DeserializeObject<Dictionary<String, Object>>(json, DefaultSettings);

		/// <summary>Deserialize a JSON string into a strongly typed object.</summary>
		/// <typeparam name="T">Target object type.</typeparam>
		/// <param name="json">JSON-formatted string.</param>
		/// <returns>Deserialized object instance.</returns>
		public static T JavaScriptDeserialize<T>(String json)
			=> String.IsNullOrEmpty(json)
				? default
				: JsonConvert.DeserializeObject<T>(json, DefaultSettings);

		/// <summary>Deserialize a JSON string into an object of the specified type.</summary>
		/// <param name="type">Target object type.</param>
		/// <param name="json">JSON-formatted string.</param>
		/// <returns>Deserialized object instance.</returns>
		public static Object JavaScriptDeserialize(Type type, String json)
			=> String.IsNullOrEmpty(json)
				? null
				: JsonConvert.DeserializeObject(json, type, DefaultSettings);

		/// <summary>Serialize an object to a JSON-formatted string.</summary>
		/// <param name="item">Object to serialize.</param>
		/// <returns>JSON-formatted string.</returns>
		public static String JavaScriptSerialize(Object item)
			=> item == null
				? null
				: JsonConvert.SerializeObject(item, DefaultSettings);
	}
}