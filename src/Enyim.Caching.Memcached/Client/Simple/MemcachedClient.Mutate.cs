using System;
using System.IO;
using System.Threading.Tasks;

namespace Enyim.Caching.Memcached
{
	public partial class MemcachedClient
	{
		public async Task<ulong?> MutateAsync(MutationMode mode, string key, ulong delta, ulong defaultValue, Expiration expiration = default)
		{
			try
			{
				var op = PerformMutate(mode, key, delta, defaultValue, cas: 0, expiration, silent: false);

				await cluster.Execute(op).ConfigureAwait(false);

				return op.StatusCode == Protocol.Status.Success
						? (ulong?)op.ResultValue
						: null;
			}
			catch (IOException)
			{
				return null;
			}
		}
	}

	public static partial class MemcachedClientExtensions
	{
		/// <summary>
		/// Increments an item's value in the cache. If the key does not exist, its value - and the result of this operation - will be set to 1.
		/// </summary>
		public static Task<ulong?> IncrementAsync(this IMemcachedClient self, string key, ulong delta = 1, ulong defaultValue = 1, in Expiration expiration = default)
			=> self.MutateAsync(MutationMode.Increment, key, delta, defaultValue, expiration);

		/// <summary>
		/// Decrements an item's value in the cache. If the key does not exist, its value - and the result of this operation - will be set to 0.
		/// </summary>
		public static Task<ulong?> DecrementAsync(this IMemcachedClient self, string key, ulong delta = 1, ulong defaultValue = 0, in Expiration expiration = default)
			=> self.MutateAsync(MutationMode.Decrement, key, delta, defaultValue, expiration);
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
