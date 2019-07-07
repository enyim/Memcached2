using System;
using System.IO;
using System.Threading.Tasks;

namespace Enyim.Caching.Memcached
{
	public partial class MemcachedClient
	{
		public async Task<OperationResult<ulong>> MutateWithResultAsync(MutationMode mode, string key, ulong delta, ulong defaultValue, ulong cas = 0, Expiration expiration = default)
		{
			try
			{
				var op = PerformMutate(mode, key, delta, defaultValue, cas, expiration, silent: false);

				await cluster.Execute(op).ConfigureAwait(false);

				return new OperationResult<ulong>(op.ResultValue, (OperationStatus)op.StatusCode, op.Cas);
			}
			catch (IOException e)
			{
				return new OperationResult<ulong>(e);
			}
		}
	}

	public static partial class MemcachedClientExtensions
	{
		public static Task<OperationResult<ulong>> IncrementWithResultAsync(this MemcachedClient self, string key, ulong delta = 1, ulong defaultValue = 0, ulong cas = 0, in Expiration expiration = default)
			=> self.MutateWithResultAsync(MutationMode.Increment, key, delta, defaultValue, cas, expiration);

		public static Task<OperationResult<ulong>> DecrementWithResultAsync(this MemcachedClient self, string key, ulong delta = 1, ulong defaultValue = 0, ulong cas = 0, in Expiration expiration = default)
			=> self.MutateWithResultAsync(MutationMode.Decrement, key, delta, defaultValue, cas, expiration);
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
