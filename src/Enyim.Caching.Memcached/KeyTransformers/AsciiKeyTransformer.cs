using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Enyim.Caching.Memcached
{
	public sealed class AsciiKeyTransformer : IKeyTransformer
	{
		private readonly MemoryPool<byte> pool;

		public AsciiKeyTransformer(MemoryPool<byte> pool)
		{
			this.pool = pool ?? throw new ArgumentNullException(nameof(pool));
		}

		public IMemoryOwner<byte> Transform(string key)
		{
			var keyLength = Encoding.ASCII.GetByteCount(key);
			var retval = pool.RentExact(keyLength); // DO NOT DISPOSE!
			var count = Encoding.ASCII.GetBytes(key, retval.Memory.Span);
			Debug.Assert(count == keyLength);

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
