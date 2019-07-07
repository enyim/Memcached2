using System;
using System.IO;
using System.Threading.Tasks;

namespace Enyim.Caching.Memcached
{
	public partial class MemcachedClient
	{
		public async Task<bool> ConcatAsync(ConcatenationMode mode, string key, ReadOnlyMemory<byte> data)
		{
			try
			{
				var op = PerformConcat(mode, key, data, cas: 0, silent: true);

				await cluster.Execute(op).ConfigureAwait(false);

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
		public static Task<bool> AppendAsync(this IMemcachedClient self, string key, in ReadOnlyMemory<byte> data)
			=> self.ConcatAsync(ConcatenationMode.Append, key, data);

		public static Task<bool> PrependAsync(this IMemcachedClient self, string key, in ReadOnlyMemory<byte> data)
			=> self.ConcatAsync(ConcatenationMode.Prepend, key, data);
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
