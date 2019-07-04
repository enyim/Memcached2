using System;
using System.IO;
using System.Threading.Tasks;

namespace Enyim.Caching.Memcached
{
	public partial class MemcachedClient
	{
		public async Task<OperationResult> StoreWithResultAsync(StoreMode mode, string key, object value, ulong cas = 0, Expiration expiration = default)
		{
			try
			{
				using var sb = new SequenceBuilder(allocator);

				var flags = transcoder.Serialize(sb, value);
				var op = await PerformStore(mode, key, flags, sb, cas, expiration, silent: false).ConfigureAwait(false);

				return new OperationResult((OperationStatus)op.StatusCode, op.Cas);
			}
			catch (IOException e)
			{
				return new OperationResult(e);
			}
		}
	}

	public static partial class MemcachedClientExtensions
	{
		public static Task<OperationResult> AddWithResultAsync(this MemcachedClient self, string key, object value, ulong cas = 0, in Expiration expiration = default)
			=> self.StoreWithResultAsync(StoreMode.Add, key, value, cas, expiration);

		public static Task<OperationResult> ReplaceWithResultAsync(this MemcachedClient self, string key, object value, ulong cas = 0, in Expiration expiration = default)
			=> self.StoreWithResultAsync(StoreMode.Replace, key, value, cas, expiration);

		public static Task<OperationResult> SetWithResultAsync(this MemcachedClient self, string key, object value, ulong cas = 0, in Expiration expiration = default)
			=> self.StoreWithResultAsync(StoreMode.Set, key, value, cas, expiration);
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
