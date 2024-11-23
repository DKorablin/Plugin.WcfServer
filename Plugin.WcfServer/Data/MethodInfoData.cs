using System.Linq;
using System.Runtime.Serialization;
using SAL.Flatbed;

namespace Plugin.WcfServer.Data
{
	public class MethodInfoData : MemberInfoData
	{
		[DataMember(Name="ReturnType")]
		public TypeInfoData ReturnType { get; private set; }

		[DataMember(Name="Parameters", EmitDefaultValue = false)]
		public ParameterData[] Parameters { get; private set; }

		internal MethodInfoData(IPluginDescription plugin, IPluginMethodInfo method)
			: base(plugin, method)
		{
			this.ReturnType = method.ReturnType == null ? null : new TypeInfoData(plugin, method.ReturnType);
			if(method.Count > 0)
				this.Parameters = method.GetParameters().Select(p => new ParameterData(plugin, p)).ToArray();
		}
	}
}