using System;
using System.Collections.Generic;

namespace Plugin.WcfServer.Extensions
{
	internal static class ListExtension
	{
		public static T Find<T>(this IList<T> list, Predicate<T> match)
		{
			_ = match ?? throw new ArgumentNullException(nameof(match));

			for(Int32 loop = 0; loop < list.Count; loop++)
				if(match(list[loop]))
					return list[loop];
			return default;
		}
	}
}