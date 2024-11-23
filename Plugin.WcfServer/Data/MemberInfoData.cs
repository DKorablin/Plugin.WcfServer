using System;
using System.Runtime.Serialization;
using SAL.Flatbed;

namespace Plugin.WcfServer.Data
{
	[DataContract(Name="Member")]
	public class MemberInfoData
	{
		/// <summary>Наименование элемента</summary>
		[DataMember(Name = "Name")]
		public String Name { get; private set; }

		/// <summary>Строковое наименование типа аргумента</summary>
		[DataMember(Name = "TypeName")]
		public String TypeName { get; private set; }

		/// <summary>Тип элемента в объекте</summary>
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