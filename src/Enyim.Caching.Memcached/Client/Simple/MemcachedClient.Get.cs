using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Enyim.Caching.Memcached
{
	public partial class MemcachedClient
	{
		public Task<T> GetAsync<T>(string key)
			=> Return<T>(PerformGetCore(key, 0, silent: true));

		public Task<T> GetAndTouchAsync<T>(string key, in Expiration expiration)
			=> Return<T>(PerformGetAndTouchCore(key, 0, expiration, silent: true));

#nullable disable
		private async Task<T> Return<T>(Task<Operations.GetOperation> performGet)
			{
			Debug.Assert(transcoder != null);

			try
			{
				var op = await performGet.ConfigureAwait(false);

				return ConvertResult<T>(op);
			}
			catch (IOException)
			{
				return default;
			}
		}

		private T ConvertResult<T>(Operations.GetOperation op)
		{
			Debug.Assert(op != null);
			Debug.Assert(transcoder != null);

			if (op.StatusCode == Protocol.Status.Success)
			{
				using var data = op.ResultData;
				Debug.Assert(data != null);

				var flags = op.ResultFlags;
				var retval = transcoder.Deserialize(data.Memory, flags);

				return (T)retval;
			}

			return default;
		}
	}
#nullable enable

	public static partial class MemcachedClientExtensions
	{
		public static Task<object> GetAsync(this IMemcachedClient self, string key)
			=> self.GetAsync<object>(key);
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
