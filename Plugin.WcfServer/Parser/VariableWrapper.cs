using System;
using System.Collections.Generic;
using System.Globalization;

namespace Plugin.WcfServer.Parser
{
	/// <summary>Information about variable for test</summary>
	[Serializable]
	internal class VariableWrapper
	{
		#region Fields
		private const String DuplicateKeyMark = "[ # ]";
		private static readonly VariableWrapper[] Empty = new VariableWrapper[0];

		private VariableWrapper[] childVariables;
		private PluginParameterWrapper currentMember;
		private readonly PluginParameterWrapper declaredMember;
		[NonSerialized]
		private readonly Boolean _isKey;
		private Boolean _isValid = true;
		private readonly Boolean _canModify = true;
		private readonly String _name;
		//[NonSerialized]
		//private readonly VariableWrapper parent;
		[NonSerialized]
		private PluginMethodWrapper _methodInfo;
		private String _value;
		#endregion Fields

		public Boolean IsValid => this._isValid;

		public EditorType EditorType
		{
			get
			{
				if(this.declaredMember.EditorType == EditorType.EditableDropDownBox)
				{
					String[] selectionList = this.GetSelectionList();
					if(selectionList == null || selectionList.Length < 1)
						return EditorType.TextBox;
				}
				return this.declaredMember.EditorType;
			}
		}
		public String FriendlyTypeName => this.declaredMember.FriendlyTypeName;

		public String Name
		{
			get
			{
				if(this._name == null)
					return this.declaredMember.VariableName;
				else if(this.IsValid)
					return this._name;
				else
					return VariableWrapper.DuplicateKeyMark;
			}
		}

		public String TypeName => this.declaredMember.TypeName;

		internal VariableWrapper(PluginParameterWrapper declaredMember, Object obj)
			: this(declaredMember)
		{
			this._value = declaredMember.GetStringRepresentation(obj);
			this._canModify = false;
		}

		internal VariableWrapper(PluginParameterWrapper declareMember, Boolean isKey)
			: this(declareMember)
		{
			this._isKey = isKey;
			if(isKey && this._value.Equals(TypeStrategy.NullRepresentation, StringComparison.Ordinal))
			{
				if(declareMember.HasMembers)
					this._value = this.TypeName;
				if(this.TypeName.Equals("System.String", StringComparison.Ordinal))
					this._value = String.Empty;
			}
		}

		internal VariableWrapper(String name, PluginParameterWrapper declaredMember)
			: this(declaredMember)
			=> this._name = name;

		internal VariableWrapper(PluginParameterWrapper declaredMember)
		{
			this.currentMember = declaredMember ?? throw new ArgumentNullException(nameof(declaredMember));
			this.declaredMember = declaredMember;
			this._value = this.currentMember.GetDefaultValue();
		}

		public IList<Int32> ValidateDictionary()
		{
			TypeStrategy.CreateAndValidateDictionary(this.TypeName, this.childVariables, out List<Int32> result);
			return result;
		}

		internal VariableWrapper[] GetChildVariables()
		{
			if(TypeStrategy.NullRepresentation.Equals(this._value, StringComparison.Ordinal))
				return VariableWrapper.Empty;

			if(this._canModify)
			{
				if(this.declaredMember.HasMembers && (this.childVariables == null || this._value != this.currentMember.TypeName))
				{
					this.currentMember = this.declaredMember;
					/*String variableName = this.declaredMember.VariableName;
					foreach(PluginTypeWrapper current in this.declaredMember.SubTypes)
					{
						if(current.TypeName.Equals(this._value))
						{
							this.currentMember = new PluginParameterWrapper(variableName, current);
							break;
						}
					}*/

					this.childVariables = new VariableWrapper[this.currentMember.Members.Count];
					/*Int32 num = 0;
					foreach(PluginParameterWrapper current2 in this.currentMember.Members)
					{
						if(this.currentMember.IsKeyValuePair() && String.Equals(current2.VariableName, "Key", StringComparison.Ordinal))
							this.childVariables[num] = new VariableWrapper(current2, true);
						else
							this.childVariables[num] = new VariableWrapper(current2);

						this.childVariables[num].SetServiceMethodInfo(this._methodInfo);
						if(this.parent != null)
							this.childVariables[num].SetParent(this);
						num++;
					}*/
				}
				if(this.declaredMember.IsContainer)
				{
					Int32 arrayLength = VariableWrapper.GetArrayLength(this._value);
					VariableWrapper[] array = this.childVariables;
					this.childVariables = new VariableWrapper[arrayLength];
					PluginParameterWrapper member = null;
					/*foreach(PluginParameterWrapper item in this.declaredMember.Members)
					{
						member = item;
						break;
					}*/

					for(Int32 i = 0; i < arrayLength; i++)
					{
						if(array != null && i < array.Length)
							this.childVariables[i] = array[i];
						else
						{
							this.childVariables[i] = new VariableWrapper("[" + i + "]", member);
							this.childVariables[i].SetServiceMethodInfo(this._methodInfo);
							/*if(this.declaredMember.IsDictionary() || this.parent != null)
							{
								this.childVariables[i].SetParent(this);
								if(this.declaredMember.IsDictionary())
									this.childVariables[i].GetChildVariables();
							}*/
						}
					}
				}
			}
			return this.childVariables;
		}

		internal Object GetObject()
		{
			this.childVariables = this.IsExpandable()
				? this.GetChildVariables()
				: null;
			return this.currentMember.GetObject(this._value, this.childVariables);
		}

		internal String[] GetSelectionList()
		{
			String[] selectionList = this.declaredMember.GetSelectionList();
			if(this._isKey && selectionList != null)
			{
				Int32 num = Array.FindIndex<String>(selectionList, new Predicate<String>(str => TypeStrategy.NullRepresentation.Equals(str, StringComparison.Ordinal)));
				if(num >= 0)
				{
					String[] array = new String[selectionList.Length - 1];
					Int32 num2 = 0;
					for(Int32 i = 0; i < array.Length; i++)
					{
						if(num2 == num)
							num2++;
						array[i] = selectionList[num2];
						num2++;
					}
					return array;
				}
			}
			return selectionList;
		}

		internal String GetValue()
		{
			if(String.Equals(this._value, this.TypeName, StringComparison.Ordinal)
				&& this.currentMember.HasMembers)
				return this.FriendlyTypeName;
			return this._value;
		}

		internal Boolean IsExpandable()
		{
			if(this.childVariables != null && this.childVariables.Length > 0)
				return true;
			if(this.EditorType == EditorType.DropDownBox)
				return !this.declaredMember.TypeName.Equals("System.Boolean")
					&& !this.declaredMember.IsEnum
					&& !TypeStrategy.NullRepresentation.Equals(this._value, StringComparison.Ordinal);

			return this.declaredMember.IsContainer
				&& !this._value.Equals(TypeStrategy.NullRepresentation, StringComparison.Ordinal)
				&& VariableWrapper.GetArrayLength(this._value) > 0;
		}

		internal void SetChildVariables(VariableWrapper[] value)
			=> this.childVariables = value;

		internal void SetServiceMethodInfo(PluginMethodWrapper serviceMethodInfo)
			=> this._methodInfo = serviceMethodInfo;

		internal ValidationResult SetValue(String userValue)
		{
			String tempValue = this._value;
			if((this._value = this.declaredMember.ValidateAndCanonicalize(userValue, out String errorMessage)) == null
				|| (this._isKey && TypeStrategy.NullRepresentation.Equals(this._value, StringComparison.Ordinal)))
			{
				this._value = tempValue;
				return new ValidationResult(false, false, errorMessage);
			}

			if(this.declaredMember.IsGeneric)
			{
				this.GetChildVariables();
				this.Validate();
			}

			Boolean refreshRequired = false;
			/*if(this.parent != null)
			{
				VariableWrapper variableInfo = this;
				while(variableInfo != null && !variableInfo._isKey)
					variableInfo = variableInfo.parent;

				if(variableInfo != null)
				{
					refreshRequired = true;
					variableInfo = variableInfo.parent.parent;
					variableInfo.Validate();
				}
			}*/

			if(this.EditorType == EditorType.EditableDropDownBox && this.declaredMember.IsContainer)
			{
				if(TypeStrategy.NullRepresentation.Equals(this._value, StringComparison.Ordinal))
				{
					if(this.declaredMember.Members.Count > 0)
						return new ValidationResult(true, true);
				} else
				{
					if(TypeStrategy.NullRepresentation.Equals(tempValue, StringComparison.Ordinal))
						return new ValidationResult(true, true);

					Int32 textLength = VariableWrapper.GetArrayLength(tempValue);
					Int32 valueLength = VariableWrapper.GetArrayLength(this._value);
					return new ValidationResult(true, textLength != valueLength);
				}
			}

			if(this.EditorType == EditorType.DropDownBox)
			{
				if(!TypeStrategy.NullRepresentation.Equals(this._value, StringComparison.Ordinal))
					return new ValidationResult(true, true);
				if(TypeStrategy.NullRepresentation.Equals(this._value, StringComparison.Ordinal) && this.currentMember.Members.Count > 0)
					return new ValidationResult(true, true);
			}
			return new ValidationResult(true, refreshRequired);
		}

		private static Int32 GetArrayLength(String canonicalizedValue)
			=> canonicalizedValue.Length > TypeStrategy.LengthRepresentation.Length
				? Int32.Parse(canonicalizedValue.Substring(TypeStrategy.LengthRepresentation.Length), CultureInfo.CurrentUICulture)
				: 0;

		private void Validate()
		{
			if(this.childVariables == null)
				return;

			foreach(VariableWrapper info in this.childVariables)
				info._isValid = true;

			//IList<Int32> list = ServiceExecutor.ValidateDictionary(this, this._methodInfo.Endpoint.ServiceProject.ClientDomain);
			IList<Int32> list = this.ValidateDictionary();
			foreach(Int32 current in list)
				this.childVariables[current]._isValid = false;
		}
	}
}