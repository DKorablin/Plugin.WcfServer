using System;
using System.Runtime.Serialization;
using SAL.Flatbed;

namespace Plugin.WcfServer.Data
{
	[DataContract]
	public class PluginData
	{
		/// <summary>Unique plugin identifier.</summary>
		/// <remarks>Currently Name acts as the unique plugin identifier.</remarks>
		[DataMember]
		public String ID { get; private set; }

		/// <summary>Plugin name.</summary>
		[DataMember]
		public String Name { get; private set; }

		/// <summary>Plugin source.</summary>
		[DataMember]
		public String Source { get; private set; }

		/// <summary>Plugin version.</summary>
		[DataMember]
		public Version Version { get; private set; }

		/// <summary>Assembly description.</summary>
		[DataMember]
		public String Description { get; private set; }

		/// <summary>Assembly company.</summary>
		[DataMember]
		public String Company { get; private set; }

		/// <summary>Plugin copyright.</summary>
		[DataMember]
		public String Copyright { get; private set; }

		internal PluginData(IPluginDescription plugin)
		{
			this.Company = plugin.Company;
			this.Copyright = plugin.Copyright;
			this.Description = plugin.Description;
			this.ID = plugin.ID;
			this.Name = plugin.Name;
			this.Source = plugin.Source;
			this.Version = plugin.Version;
		}
	}
}