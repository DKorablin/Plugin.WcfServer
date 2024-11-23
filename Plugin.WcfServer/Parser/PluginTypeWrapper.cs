using System;
using System.Collections.Generic;
using System.Text;
using SAL.Flatbed;
using Plugin.WcfServer.Extensions;

namespace Plugin.WcfServer.Parser
{
	internal class PluginTypeWrapper
	{
		private static Dictionary<String, PluginTypeWrapper> _typeCache = new Dictionary<String, PluginTypeWrapper>();
		private readonly IPluginTypeInfo _info;

		private String _friendlyName;
		private TypeStrategy _typeStrategy;

		public EditorType EditorType => this._typeStrategy.EditorType;

		public String FriendlyName
		{
			get
			{
				if(this._friendlyName == null)
					this.ComposeFriendlyName();
				return this._friendlyName;
			}
		}

		public Boolean IsValid => this._typeStrategy.IsValid;

		public ICollection<PluginTypeWrapper> Members { get; private set; }

		private ICollection<PluginTypeWrapper> SubTypes { get; set; }

		public String TypeName => this._typeStrategy.TypeName;

		private PluginTypeWrapper(IPluginTypeInfo info)
		{
			this._info = info ?? throw new ArgumentNullException(nameof(info));

			TypeProperty abilities = new TypeProperty(info);
			this._typeStrategy = new TypeStrategy(info.TypeName, abilities, info.GetDefaultValues());
			this.SubTypes = new List<PluginTypeWrapper>();
			this.Members = new List<PluginTypeWrapper>();

			if(this._typeStrategy.IsGeneric)
				foreach(IPluginTypeInfo member in info.GenericMembers)
					this.SubTypes.Add(PluginTypeWrapper.GetTypeWrapper(member));

			foreach(IPluginMemberInfo member in info.Members)
			{
				IPluginTypeInfo subType = member as IPluginTypeInfo;
				if(subType != null)
					this.Members.Add(PluginTypeWrapper.GetTypeWrapper(subType));
			}
		}

		private PluginTypeWrapper(IPluginTypeInfo info, Type localType)
		{
			this._info = info?? throw new ArgumentNullException(nameof(info));

			TypeProperty abilities = new TypeProperty(localType);
			this._typeStrategy = new TypeStrategy(info.TypeName, abilities, info.GetDefaultValues());
			this.SubTypes = new List<PluginTypeWrapper>();
			this.Members = new List<PluginTypeWrapper>();

			if(this._typeStrategy.IsGeneric)
				foreach(IPluginTypeInfo member in info.GenericMembers)
					this.SubTypes.Add(PluginTypeWrapper.GetTypeWrapper(member));

			if(!localType.IsBclType())
				throw new NotImplementedException();
		}

		internal static PluginTypeWrapper GetTypeWrapper(IPluginTypeInfo typeInfo)
		{
			PluginTypeWrapper result = _typeCache.TryGetValue(typeInfo.TypeName, out result) ? result : null;
			if(result == null)
			{
				Type localType = TypeStrategy.GetClientType(typeInfo.TypeName);
				if(localType == null)
					result = new PluginTypeWrapper(typeInfo);
				else
					result = new PluginTypeWrapper(typeInfo, localType);

				_typeCache.Add(typeInfo.TypeName, result);
			}
			return result;
		}

		public String GetDefaultValue()
			=> this._typeStrategy.GetDefaultValue();

		public Int32 GetEnumMemberCount()
			=> this._typeStrategy.GetEnumMemberCount();

		public Object GetObject(String value, VariableWrapper[] variables)
			=> this._typeStrategy.GetObject(value, variables);

		public String[] GetSelectionList()
		{
			String[] array = this._typeStrategy.GetSelectionList();
			if(array != null && array.Length == 0)
			{
				List<String> list = new List<String>();
				list.Add(TypeStrategy.NullRepresentation);
				list.Add(this._typeStrategy.TypeName);
				/*foreach(PluginTypeWrapper current in this.SubTypes)
				{
					if(current.IsValid)
					{
						list.Add(current.TypeName);
					}
				}*/
				array = new String[list.Count];
				list.CopyTo(array);
			}
			return array;
		}

		public String GetStringRepresentation(Object obj)
			=> this._typeStrategy.GetStringRepresentation(obj);

		public Boolean HasMembers => this._typeStrategy.HasMembers;

		public Boolean IsContainer => this._typeStrategy.IsContainer;

		public Boolean IsGeneric => this._typeStrategy.IsGeneric;

		public Boolean IsEnum => this._typeStrategy.IsEnum;

		public Boolean IsKeyValuePair => this._typeStrategy.IsKeyValuePair;

		public Boolean IsStruct => this._typeStrategy.IsStruct;

		public void MarkAsInvalid()
			=> this._typeStrategy.MarkAsInvalid();

		public String ValidateAndCanonicalize(String input, out Int32 length)
			=> this._typeStrategy.ValidateAndCanonicalize(input, out length);

		private void ComposeFriendlyName()
		{
			Int32 num = this.TypeName.IndexOf('`');
			if(num > -1)
			{
				StringBuilder stringBuilder = new StringBuilder(this.TypeName.Substring(0, num));
				stringBuilder.Append("<");
				ICollection<PluginTypeWrapper> collection = this.Members;
				if(this.IsGeneric)
					collection = this.SubTypes;
				Int32 num2 = 0;
				foreach(PluginTypeWrapper current in collection)
				{
					if(num2++ > 0)
						stringBuilder.Append(",");
					stringBuilder.Append(current.FriendlyName);
				}
				stringBuilder.Append(">");
				this._friendlyName = stringBuilder.ToString();
				return;
			}
			this._friendlyName = this.TypeName;
		}
	}
}