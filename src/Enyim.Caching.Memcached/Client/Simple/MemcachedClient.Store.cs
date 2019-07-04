using System;
using System.IO;
using System.Threading.Tasks;

namespace Enyim.Caching.Memcached
{
	public partial class MemcachedClient
	{
		public async Task<bool> StoreAsync(StoreMode mode, string key, object value, Expiration expiration = default)
		{
			try
			{
				using var sb = new SequenceBuilder(allocator);

				var flags = transcoder.Serialize(sb, value);
				var op = await PerformStore(mode, key, flags, sb, 0, expiration, silent: true).ConfigureAwait(false);

				return op.StatusCode == Protocol.Status.Success;
			}
			catch (IOException)
			{
				return false;
			}
		}
	}

	public static partial class MemcachedClientExtensions
	{
		public static Task<bool> AddAsync(this IMemcachedClient self, string key, object value, in Expiration expiration = default)
			=> self.StoreAsync(StoreMode.Add, key, value, expiration);

		public static Task<bool> ReplaceAsync(this IMemcachedClient self, string key, object value, in Expiration expiration = default)
			=> self.StoreAsync(StoreMode.Replace, key, value, expiration);

		public static Task<bool> SetAsync(this IMemcachedClient self, string key, object value, in Expiration expiration = default)
			=> self.StoreAsync(StoreMode.Set, key, value, expiration);
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
