using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using SAL.Flatbed;

namespace Plugin.WcfServer.Data
{
	[DataContract(Name="Type")]
	public class TypeInfoData : MemberInfoData
	{
		/// <summary>Массив публичных свойств и методов</summary>
		//[DataMember(Name="Members", EmitDefaultValue = false)]
		public MemberInfoData[] Members { get; private set; }

		/// <summary>Массив генериков</summary>
		[DataMember(Name = "GenericMembers", EmitDefaultValue = false)]
		public TypeInfoData[] GenericMembers { get; private set; }

		/// <summary>Элемент является массивом</summary>
		[DataMember(Name = "IsArray")]
		public Boolean IsArray { get; private set; }

		/// <summary>Элемент является генериком</summary>
		[DataMember(Name = "IsGeneric")]
		public Boolean IsGeneric { get; private set; }

		/// <summary>Элемент может принимать null</summary>
		[DataMember(Name = "IsValueType")]
		public Boolean IsValueType { get; private set; }

		/// <summary>Получить значения по умолчанию для элемента (Енум или значение по умолчанию для входчщего в метод параметра)</summary>
		[DataMember(Name = "DefaultValues", EmitDefaultValue = false)]
		public String[] DefaultValues { get; private set; }

		internal TypeInfoData(IPluginDescription plugin, IPluginTypeInfo info)
			: base(plugin, info)
		{
			this.IsArray = info.IsArray;
			this.IsGeneric = info.IsGeneric;
			this.IsValueType = info.IsValueType;
			this.DefaultValues = info.GetDefaultValues();

			List<MemberInfoData> members = new List<MemberInfoData>();
			foreach(IPluginMemberInfo member in info.Members)
			{
				switch(member.MemberType)
				{
				case MemberTypes.Method:
					members.Add(new MethodInfoData(plugin, (IPluginMethodInfo)member));
					break;
				default:
					members.Add(new MemberInfoData(plugin, member));
					break;
				}
			}

			if(members.Count > 0)
				this.Members = members.ToArray();

			this.GenericMembers = info.GenericMembers.Select(p => new TypeInfoData(plugin, p)).ToArray();
			if(this.GenericMembers.Length == 0)
				this.GenericMembers = null;
		}
	}
}