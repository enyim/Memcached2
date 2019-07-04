﻿using System;
using System.Linq;
using System.Threading.Tasks;

namespace Enyim.Caching.Memcached
{
	public partial class MemcachedClient
	{
		private async Task<Operations.MutateOperation> PerformMutate(
			MutationMode mode, string key,
			ulong delta, ulong defaultValue,
			ulong cas, Expiration expiration, bool silent)
		{
			using var realKey = keyTransformer.Transform(key);

			var op = new Operations.MutateOperation(allocator, realKey.Memory, mode, delta, defaultValue)
			{
				Cas = cas,
				Expiration = expiration,
				Silent = silent
			};

			// must wait for the operation to finish, otherwide the key would be disposed earlier than the op was ran
			await cluster.Execute(op).ConfigureAwait(false);

			return op;
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
