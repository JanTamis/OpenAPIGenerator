using System;
using System.Collections.Generic;

namespace OpenAPIGenerator.Extensions;

public static class EnumerableExtensions
{
	public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)
	{
		var seenKeys = new HashSet<TKey>();
		
		foreach (var element in source)
		{
			if (seenKeys.Add(keySelector(element)))
			{
				yield return element;
			}
		}
	}

	public static IEnumerable<T> Index<T>(this IEnumerable<T> source, Func<T, int, T> selector)
	{
		var index = 0;

		foreach (var item in source)
		{
			yield return selector(item, index++);
		}
	}
}