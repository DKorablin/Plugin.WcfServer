using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.CSharp;

namespace Plugin.WcfServer.Parser
{
	internal static class StringFormatter
	{
		public static String FromEscapeCode(String input)
		{
			Char[] array = input.ToCharArray();
			StringBuilder result = new StringBuilder();
			Int32 i = 0;
			Int32 num = 0;
			while(i < array.Length)
			{
				Char c = array[i];
				if(num == 0)
				{
					if(array[i] == '\\')
						num = 1;
					else
					{
						num = 0;
						result.Append(c);
					}
				} else
				{
					if(num != 1)
						return null;

					num = 0;
					switch(c)
					{
					case 'r':
						result.Append('\r');
						break;
					case 'n':
						result.Append('\n');
						break;
					case 't':
						result.Append('\t');
						break;
					default:
						if(c != '\\')
							return null;
						result.Append('\\');
						break;
					}
				}
				i++;
			}
			if(num == 0)
				return result.ToString();
			return null;
		}

		public static String ToEscapeCode(String input)
		{
			StringWriter stringWriter = new StringWriter(CultureInfo.CurrentUICulture);
			CSharpCodeProvider cSharpCodeProvider = new CSharpCodeProvider();
			cSharpCodeProvider.GenerateCodeFromExpression(new CodePrimitiveExpression(input), stringWriter, new CodeGeneratorOptions());
			return stringWriter.ToString();
		}
	}
}
