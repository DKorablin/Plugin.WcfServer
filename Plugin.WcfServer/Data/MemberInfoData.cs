using System;
using System.Runtime.Serialization;
using SAL.Flatbed;

namespace Plugin.WcfServer.Data
{
	[DataContract(Name="Member")]
	public class MemberInfoData
	{
		/// <summary>Gets or sets the mane of the member.</summary>
		[DataMember(Name = "Name")]
		public String Name { get; private set; }

		/// <summary>Gets or sets member type name.</summary>
		[DataMember(Name = "TypeName")]
		public String TypeName { get; private set; }

		/// <summary>The type of the member in the object.</summary>
		/// <remarks>System.Reflection.MemberTypes</remarks>
		[DataMember(Name = "MemberType")]
		public String MemberType { get; private set; }

		internal MemberInfoData(IPluginDescription plugin, IPluginMemberInfo info)
		{
			this.Name = info.Name;
			this.TypeName = info.TypeName;
			this.MemberType = info.MemberType.ToString();
		}
	}
}