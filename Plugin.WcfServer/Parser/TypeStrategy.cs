using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using Plugin.WcfServer.Extensions;

namespace Plugin.WcfServer.Parser
{
	[Serializable]
	internal class TypeStrategy
	{
		public const String LengthRepresentation = "length=";
		public const String NullRepresentation = "(null)";
		internal static readonly IDictionary<String, Type> typesCache = new Dictionary<String, Type>();
		private static readonly List<String> numericTypes = new List<String>(new String[]
		{
			typeof(Int16).FullName,
			typeof(Int32).FullName,
			typeof(Int64).FullName,
			typeof(UInt16).FullName,
			typeof(UInt32).FullName,
			typeof(UInt64).FullName,
			typeof(Byte).FullName,
			typeof(SByte).FullName,
			typeof(Single).FullName,
			typeof(Double).FullName,
			typeof(Decimal).FullName,
		});

		private String[] _enumChoices;

		private TypeProperty _typeProperty;

		public EditorType EditorType { get; private set; }

		public Boolean IsValid { get; private set; }

		public String TypeName { get; private set; }

		private Type ClientType => TypeStrategy.GetClientType(this.TypeName);

		public TypeStrategy(String typeName, TypeProperty typeProperty, String[] enumChoices)
		{
			this.TypeName = typeName;
			this._enumChoices = enumChoices;
			this.IsValid = true;
			this._typeProperty = typeProperty;
			if(TypeStrategy.numericTypes.Contains(typeName))
				this.EditorType = EditorType.TextBox;
			else
				switch(this.TypeName)
				{
				case "System.Boolean":
					this.EditorType = EditorType.DropDownBox;
					return;
				case "System.Char":
					this.EditorType = EditorType.TextBox;
					return;
				case "System.Guid":
					this.EditorType = EditorType.TextBox;
					return;
				case "System.String":
					this.EditorType = EditorType.EditableDropDownBox;
					return;
				case "System.DateTime":
					this.EditorType = EditorType.TextBox;
					return;
				case "System.DateTimeOffset":
					this.EditorType = EditorType.TextBox;
					return;
				case "System.TimeSpan":
					this.EditorType = EditorType.TextBox;
					return;
				case "System.Uri":
					this.EditorType = EditorType.EditableDropDownBox;
					return;
				case "System.Xml.XmlQualifiedName":
					this.EditorType = EditorType.EditableDropDownBox;
					return;
				default:
					if(this.IsContainer)
						this.EditorType = EditorType.EditableDropDownBox;
					else if(this.HasMembers)
						this.EditorType = EditorType.DropDownBox;
					else if(enumChoices != null)
						this.EditorType = EditorType.DropDownBox;
					else
						this.IsValid = typeName.Equals(typeof(NullObject).FullName, StringComparison.Ordinal);
					return;
				}
		}

		public static Object CreateAndValidateDictionary(String typeName, VariableWrapper[] variables, out List<Int32> invalidList)
		{
			Type type = TypeStrategy.typesCache[typeName];
			Object result = Activator.CreateInstance(type);
			invalidList = new List<Int32>(variables.Length);

			if(variables != null)
			{
				MethodInfo method = type.GetMethod("Add");
				if(method == null)
					return null;

				Int32 num = 0;
				foreach(VariableWrapper info in variables)
				{
					if(info != null && info.IsValid)
					{
						VariableWrapper[] childVariables = info.GetChildVariables();
						Object[] parameters = new Object[]
						{
							childVariables[0].GetObject(),
							childVariables[1].GetObject(),
						};
						try
						{
							method.Invoke(result, parameters);
						} catch(TargetInvocationException)
						{
							invalidList.Add(num);
						}
						num++;
					}
				}
			}
			return result;
		}

		public String GetDefaultValue()
		{
			if(this._enumChoices != null)
				return this._enumChoices[0];
			else if(TypeStrategy.numericTypes.Contains(this.TypeName))
				return "0";
			else
				switch(this.TypeName)
				{
				case "System.Boolean":
					return Boolean.FalseString;
				case "System.Char":
					return "A";
				case "System.Guid":
					return Guid.NewGuid().ToString();
				case "System.DateTime":
					return DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString();
				case "System.DateTimeOffset":
					return DateTimeOffset.Now.ToString();
				case "System.TimeSpan":
					return TimeSpan.Zero.ToString();
				case "System.Uri":
					return "http://localhost";
				case "System.Xml.XmlQualifiedName":
					return "namespace:name";
				default:
					if(this.IsContainer)
						return "length=0";
					else if(this._typeProperty.IsKeyValuePair || this._typeProperty.IsStruct)
						return this.TypeName;
					else
						return TypeStrategy.NullRepresentation;
				}
		}

		public Int32 GetEnumMemberCount()
		{
			if(this._enumChoices != null)
				return this._enumChoices.Length;
			return 0;
		}

		public String ValidateAndCanonicalize(String input, out Int32 lengthValue)
		{
			lengthValue = -1;
			String text = input;
			if(TypeStrategy.numericTypes.Contains(this.TypeName))
			{
				Type type = Type.GetType(this.TypeName);
				Object[] array = new Object[2];
				array[0] = input;
				Object[] array2 = array;
				MethodInfo method = type.GetMethod("TryParse", new Type[]
				{
					typeof(String),
					Type.GetType(this.TypeName + "&")
				});
				Boolean isValid = (Boolean)method.Invoke(null, array2);
				if(isValid)
					return array2[1].ToString();

				return null;
			} else
			{
				switch(this.TypeName)
				{
				case "System.Boolean":
					Boolean dummy;
					return Boolean.TryParse(input, out dummy) ? text : null;
				case "System.Char":
					return input.Length == 1 ? text : null;
				case "System.Guid":
					try
					{
						return new Guid(input).ToString();
					} catch(FormatException)
					{
						return null;
					}
				case "System.Uri":
					if(input.Equals(TypeStrategy.NullRepresentation, StringComparison.Ordinal))
						return text;

					Uri uri;
					return Uri.TryCreate(input, UriKind.Absolute, out uri) ? uri.ToString() : null;
				case "System.DateTime":
					try
					{
						return new DateTimeConverter().ConvertFrom(input).ToString();
					} catch(FormatException)
					{
						return null;
					}
				case "System.DateTimeOffset":
					return DateTimeOffset.TryParse(input, out DateTimeOffset offset) ? offset.ToString() : null;
				case "System.TimeSpan":
					try
					{
						return new TimeSpanConverter().ConvertFrom(input).ToString();
					} catch(FormatException)
					{
						return null;
					}
				case "System.String":
					return StringFormatter.FromEscapeCode(text) == null ? null : text;
				default:
					if(!this.IsContainer)
						return text;
					if(input.Equals(TypeStrategy.NullRepresentation))
						return text;
					if(!input.TrimStart(new Char[] { ' ' }).StartsWith("length", StringComparison.OrdinalIgnoreCase))
						return null;

					text = input.Replace(" ", "");
					if(text.StartsWith(TypeStrategy.LengthRepresentation, StringComparison.OrdinalIgnoreCase))
					{
						input = text.Substring(TypeStrategy.LengthRepresentation.Length);
						if(Int32.TryParse(input, out lengthValue) && lengthValue >= 0)
							return TypeStrategy.LengthRepresentation + input;
					}
					return null;
				}
			}
		}

		public Object GetObject(String value, VariableWrapper[] variables)
		{//TODO: Validation method: ServiceMemberInfo.ValidateAndCanonicalize(String, out String) parsing method: TypeStrategy.GetObject(String, VariableInfo[])
			if(this._enumChoices != null)
				return Enum.Parse(this.ClientType, value);
			else if(TypeStrategy.numericTypes.Contains(this.TypeName))
			{
				MethodInfo parse = Type.GetType(this.TypeName).GetMethod("Parse", new Type[] { typeof(String), });
				return parse.Invoke(null, new Object[] { value, });
				//TODO: Call Parse(value,CultureInfo.CurrentUICulture) replaced because Decimal (100,21 not accepted by parser, 100.21 not accepted by VirtualTreeGrid)
				// Validation occurs in method: ValidateAndCanonicalize
				//return Type.GetType(this.TypeName).GetMethod("Parse", new Type[] { typeof(String), typeof(IFormatProvider) }).Invoke(null, new Object[] { value, CultureInfo.CurrentUICulture, });
			} else
			{
				switch(this.TypeName)
				{
				case "System.Boolean":
					return Boolean.Parse(value);
				case "System.Char":
					return value[0];
				case "System.Guid":
					return new Guid(value);
				case "System.String":
					return value.Equals(TypeStrategy.NullRepresentation, StringComparison.Ordinal) ? null : StringFormatter.FromEscapeCode(value);
				case "System.DateTime":
					return new DateTimeConverter().ConvertFrom(value);
				case "System.DateTimeOffset":
					return DateTimeOffset.Parse(value, CultureInfo.CurrentUICulture);
				case "System.TimeSpan":
					return new TimeSpanConverter().ConvertFrom(value);
				case "System.Uri":
					return value.Equals(TypeStrategy.NullRepresentation, StringComparison.Ordinal) ? null : new Uri(value);
				default:
					if(value.Equals(TypeStrategy.NullRepresentation))
						return null;
					else if(this._typeProperty.IsArray)
					{
						Type type = TypeStrategy.GetClientType(this.TypeName.Substring(0, this.TypeName.Length - 2));
						Array array = Array.CreateInstance(type, Int32.Parse(value.Substring(TypeStrategy.LengthRepresentation.Length), CultureInfo.CurrentUICulture));
						Int32 num = 0;
						if(variables != null)
							for(Int32 i = 0; i < variables.Length; i++)
							{
								VariableWrapper info = variables[i];
								array.SetValue(info.GetObject(), num++);
							}
						return array;
					} else if(this._typeProperty.IsCollection)
					{
						Type type = this.ClientType;
						Object obj = Activator.CreateInstance(type);
						if(variables != null)
						{
							MethodInfo method = type.GetMethod("Add");
							for(Int32 j = 0; j < variables.Length; j++)
							{
								VariableWrapper info = variables[j];
								method.Invoke(obj, new Object[] { info.GetObject(), });
							}
						}
						return obj;
					} else if(this._typeProperty.IsDictionary)
						return TypeStrategy.CreateAndValidateDictionary(this.TypeName, variables, out _);
					else if(this._typeProperty.IsNullable)
						return variables[0].GetObject();
					else if(this._typeProperty.IsKeyValuePair)
					{
						Type type = TypeStrategy.typesCache[this.TypeName];
						return Activator.CreateInstance(type, new Object[]
							{
								variables[0].GetObject(),
								variables[1].GetObject()
							});
					} else
					{
						Type type = this.ClientType;
						Object obj2 = Activator.CreateInstance(type);
						for(Int32 k = 0; k < variables.Length; k++)
						{
							VariableWrapper info = variables[k];
							PropertyInfo property = type.GetProperty(info.Name);
							if(property != null)
								property.SetValue(obj2, info.GetObject(), null);
							else
							{
								FieldInfo field = type.GetField(info.Name);
								field.SetValue(obj2, info.GetObject());
							}
						}
						return obj2;
					}
				}
			}
		}

		public String[] GetSelectionList()
		{
			if(this.EditorType == EditorType.EditableDropDownBox)
				return new String[] { TypeStrategy.NullRepresentation };
			else if(this.EditorType != EditorType.DropDownBox)
				return null;
			else if(this.TypeName.Equals("System.Boolean"))
				return new String[] { Boolean.TrueString, Boolean.FalseString, };
			else if(this._enumChoices != null)
				return this._enumChoices;
			else if(this._typeProperty.IsKeyValuePair || this._typeProperty.IsStruct)
				return new String[] { this.TypeName, };
			else
				return new String[] { };
		}

		public String GetStringRepresentation(Object obj)
		{
			if(obj == null)
				return TypeStrategy.NullRepresentation;
			else if(this.EditorType == EditorType.DropDownBox)
				return obj.GetType().Equals(typeof(Boolean)) || this._enumChoices != null
					? obj.ToString()
					: String.Empty;
			else if(obj.GetType().IsArray)
				return TypeStrategy.LengthRepresentation + ((Array)obj).Length;
			else if(obj.GetType().IsDictionaryType() || obj.GetType().IsCollectionType())
				return TypeStrategy.LengthRepresentation + ((ICollection)obj).Count;
			else if(obj is String)
				return StringFormatter.ToEscapeCode(obj.ToString());
			else
				return obj.ToString();
		}

		public Boolean HasMembers => this._typeProperty.IsGeneric || this._typeProperty.IsNullable || this._typeProperty.IsKeyValuePair;

		public Boolean IsContainer => this._typeProperty.IsArray || this._typeProperty.IsDictionary || this._typeProperty.IsCollection;

		public Boolean IsGeneric => this._typeProperty.IsGeneric;

		public Boolean IsEnum => this._enumChoices != null;

		public Boolean IsKeyValuePair => this._typeProperty.IsKeyValuePair;

		public Boolean IsStruct => this._typeProperty.IsStruct;

		public void MarkAsInvalid()
			=> this.IsValid = false;

		internal static Type GetClientType(String typeName)
			=> Type.GetType(typeName, false);
	}
}