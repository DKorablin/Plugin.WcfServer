using System;
using System.Runtime.Serialization;
using SAL.Flatbed;

namespace Plugin.WcfServer.Data
{
	public class ParameterData : TypeInfoData
	{
		/// <summary>Gets or sets the identifier that current member is output.</summary>
		[DataMember(Name = "IsOut")]
		public Boolean IsOut { get; private set; }

		internal ParameterData(IPluginDescription plugin, IPluginParameterInfo parameter)
			: base(plugin, parameter)
		{
			this.IsOut = parameter.IsOut;
		}
	}
}