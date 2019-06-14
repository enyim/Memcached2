using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Enyim.Caching.Memcached
{
	using StatDictionary = Dictionary<IPEndPoint, IReadOnlyDictionary<string, string>>;

	/// <summary>
	/// Represents the statistics of a Memcached node.
	/// </summary>
	public class MemcachedStats
	{
		internal static MemcachedStats Empty = new MemcachedStats();

		private readonly StatDictionary data = new StatDictionary();

		public IReadOnlyDictionary<IPEndPoint, IReadOnlyDictionary<string, string>> Data => data;

		internal void Set(IPEndPoint endpoint, IReadOnlyDictionary<string, string> stats) => data[endpoint] = stats;

		public string GetValue(IPEndPoint server, string key)
		{
			if (!data.TryGetValue(server, out var dict))
				throw new KeyNotFoundException($"No stats are available for {server}");

			if (!dict.TryGetValue(key, out var value))
				throw new KeyNotFoundException($"Stat value {key} does not exist");

			return value;
		}

		public bool TryGetValue(IPEndPoint server, string key, out string value)
		{
			value = "";

			return data.TryGetValue(server, out var dict)
					&& dict.TryGetValue(key, out value);
		}

		public IEnumerable<string> GetValues(string key)
		{
			foreach (var dict in data)
			{
				if (dict.Value.TryGetValue(key, out var retval))
					yield return retval;
			}
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
