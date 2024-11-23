using System;
using System.Runtime.Serialization;
using SAL.Flatbed;

namespace Plugin.WcfServer.Data
{
	public class ParameterData : TypeInfoData
	{
		/// <summary>Исходящий параметр</summary>
		[DataMember(Name="IsOut")]
		public Boolean IsOut { get; private set; }

		internal ParameterData(IPluginDescription plugin, IPluginParameterInfo parameter)
			: base(plugin, parameter)
		{
			this.IsOut = parameter.IsOut;
		}
	}
}