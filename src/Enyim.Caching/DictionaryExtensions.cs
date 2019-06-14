using System;
using System.Collections.Generic;

namespace Enyim.Caching
{
	internal static class DictionaryExtensions
	{
		public static Dictionary<TKey, TValue> IntoDictionary<TItem, TKey, TValue>(this IEnumerable<TItem> self, Dictionary<TKey, TValue> target, Func<TItem, TKey> keySelector, Func<TItem, TValue> valueSelector)
		{
			foreach (var item in self)
			{
				target[keySelector(item)] = valueSelector(item);
			}

			return target;
		}
	}
}

#region [ License information          ]

/*

Copyright (c) Attila Kiskó, enyim.com

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

  http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

*/

#endregion
