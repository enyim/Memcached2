using System;
using System.Buffers;
using System.Linq;
using System.Threading.Tasks;
using Enyim.Caching.Memcached.Operations;

namespace Enyim.Caching.Memcached
{
	public partial class MemcachedClient
	{
		private ConcatOperation PerformConcat(ConcatenationMode mode, string key, ReadOnlyMemory<byte> data, ulong cas, bool silent)
		{
			var retval = new ConcatOperation(allocator, key, keyFormatter, mode, data)
			{
				Cas = cas,
				Silent = silent
			};

			retval.Initialize();

			return retval;
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
