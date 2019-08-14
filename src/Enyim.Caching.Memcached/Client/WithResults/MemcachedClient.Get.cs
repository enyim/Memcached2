using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;

namespace Enyim.Caching.Memcached
{
	public partial class MemcachedClient
	{
		public Task<OperationResult<T>> GetWithResultAsync<T>(string key, ulong cas = 0)
			=> ReturnWithResult<T>(PerformGetCore(key, cas, silent: false));

		public Task<OperationResult<T>> GetAndTouchWithResultAsync<T>(string key, in Expiration expiration, ulong cas = 0)
			=> ReturnWithResult<T>(PerformGetAndTouchCore(key, cas, expiration, silent: false));

		private async Task<OperationResult<T>> ReturnWithResult<T>(Operations.GetOperationBase op)
		{
			Debug.Assert(itemFormatter != null);

			try
			{
				await cluster.Execute(op).ConfigureAwait(false);

				T retval = default;

				if (op.StatusCode == Protocol.Status.Success)
				{
					using var data = op.ResultData;
					Debug.Assert(data != null);

					retval = (T)itemFormatter.Deserialize(data.Memory, op.ResultFlags);
				}

				return new OperationResult<T>(retval, (OperationStatus)op.StatusCode, op.Cas);
			}
			catch (IOException e)
			{
				return new OperationResult<T>(e);
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
