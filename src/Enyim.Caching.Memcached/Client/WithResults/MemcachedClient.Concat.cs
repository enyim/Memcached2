using System;
using System.IO;
using System.Threading.Tasks;

namespace Enyim.Caching.Memcached
{
	public partial class MemcachedClient
	{
		public async Task<OperationResult> ConcatWithResultAsync(ConcatenationMode mode, string key, ReadOnlyMemory<byte> data, ulong cas = 0)
		{
			try
			{
				var op = PerformConcat(mode, key, data, cas, silent: false);

				await cluster.Execute(op).ConfigureAwait(false);

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
		public static Task<OperationResult> AppendWithResultAsync(this MemcachedClient self, string key, in ReadOnlyMemory<byte> data, ulong cas = 0)
			=> self.ConcatWithResultAsync(ConcatenationMode.Append, key, data, cas);

		public static Task<OperationResult> PrependWithResultAsync(this MemcachedClient self, string key, in ReadOnlyMemory<byte> data, ulong cas = 0)
			=> self.ConcatWithResultAsync(ConcatenationMode.Prepend, key, data, cas);
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
