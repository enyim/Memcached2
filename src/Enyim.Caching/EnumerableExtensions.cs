using System;
using System.Collections.Generic;

namespace Caching
{
    public static class EnumerableExtensions
    {
#if NETSTANDARD2_0
		public static HashSet<TSource> ToHashSet<TSource>(this IEnumerable<TSource> source)
		    => source.ToHashSet(null!);

	    public static HashSet<TSource> ToHashSet<TSource>(this IEnumerable<TSource> source, IEqualityComparer<TSource> comparer) =>
		    source == null!
			    ? throw new ArgumentNullException(nameof(source))
			    : new HashSet<TSource>(source, comparer);
#endif
	}
}
