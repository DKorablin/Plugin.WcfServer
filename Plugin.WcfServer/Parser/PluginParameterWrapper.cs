using System;
using System.Collections.Generic;
using System.Globalization;
using SAL.Flatbed;

namespace Plugin.WcfServer.Parser
{
	internal class PluginParameterWrapper : IComparable
	{
		private readonly IPluginParameterInfo _parameter;

		private readonly PluginTypeWrapper _type;

		#region Properties
		public EditorType EditorType => this._type.EditorType;

		public String FriendlyTypeName => this._type.FriendlyName;

		public Boolean IsValid => this._type.IsValid;

		public ICollection<PluginTypeWrapper> Members => this._type.Members;

		//public ICollection<PluginTypeWrapper> SubTypes => this._type.SubTypes;

		public String TypeName => this._type.TypeName;

		public String VariableName => this._parameter.Name;
		#endregion Properties

		public PluginParameterWrapper(IPluginParameterInfo parameter)
		{
			this._parameter = parameter?? throw new ArgumentNullException(nameof(parameter));
			this._type = PluginTypeWrapper.GetTypeWrapper(parameter);

			foreach(IPluginMemberInfo member in parameter.Members)
				if(member is IPluginTypeInfo subType)
					this.Members.Add(PluginTypeWrapper.GetTypeWrapper(subType));
		}

		public Int32 CompareTo(Object obj)
		{
			PluginParameterWrapper serviceMemberInfo = (PluginParameterWrapper)obj;
			return this._parameter.Name.CompareTo(serviceMemberInfo._parameter.Name);
		}

		public String GetDefaultValue()
			=> this._type.GetDefaultValue();

		public Object GetObject(String value, VariableWrapper[] variables)
			=> this._type.GetObject(value, variables);

		public String[] GetSelectionList()
			=> this._type.GetSelectionList();

		public String GetStringRepresentation(Object obj)
			=> this._type.GetStringRepresentation(obj);

		public Boolean HasMembers => this._type.HasMembers;

		public Boolean IsContainer => this._type.IsContainer;

		public Boolean IsGeneric => this._type.IsGeneric;

		public Boolean IsEnum => this._type.IsEnum;

		public Boolean IsStruct => this._type.IsStruct;

		public String ValidateAndCanonicalize(String value, out String errorMessage)
		{
			String text = this._type.ValidateAndCanonicalize(value, out _);
			errorMessage = null;

			if(text == null)
				errorMessage = $"'{value}' is not valid value for this type";
			return text;
		}
	}
}