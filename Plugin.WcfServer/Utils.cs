using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Plugin.WcfServer
{
	internal static class Utils
	{
		/// <summary>Check if exception is fatal, after which further code execution is impossible</summary>
		/// <param name="exception">Exception to check</param>
		/// <returns>Exception is fatal</returns>
		public static Boolean IsFatal(Exception exception)
		{
			while(exception != null)
			{
				if((exception is OutOfMemoryException && !(exception is InsufficientMemoryException)) || exception is ThreadAbortException || exception is AccessViolationException || exception is SEHException)
					return true;
				if(!(exception is TypeInitializationException) && !(exception is TargetInvocationException))
					break;
				exception = exception.InnerException;
			}
			return false;
		}

		public static UInt32[] BitToInt(params Boolean[] bits)
		{
			UInt32[] result = new UInt32[] { };
			Int32 counter = 0;
			for(Int32 loop = 0; loop < bits.Length; loop++)
			{
				if(result.Length <= loop)//Increase array by one if value does not fit
					Array.Resize<UInt32>(ref result, result.Length + 1);

				for(Int32 innerLoop = 0; innerLoop < 32; innerLoop++)
				{
					result[loop] |= Convert.ToUInt32(bits[counter++]) << innerLoop;
					if(counter >= bits.Length)
						break;
				}
				if(counter >= bits.Length)
					break;
			}
			return result;
		}

		/// <summary>Get a deterministic hash code for a string that is consistent across .NET Framework and .NET Core/.NET 5+</summary>
		/// <param name="value">The string to hash</param>
		/// <returns>A deterministic 32-bit hash code</returns>
		public static Int32 GetDeterministicHashCode(String value)
		{
			if(String.IsNullOrEmpty(value))
				return 0;

			using(MD5 md5 = MD5.Create())
			{
				Byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(value));
				// Take first 4 bytes and convert to Int32
				return BitConverter.ToInt32(hash, 0);
			}
		}
	}
}