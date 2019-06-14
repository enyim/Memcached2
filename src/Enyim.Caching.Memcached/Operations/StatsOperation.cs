using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Enyim.Caching.Memcached.Operations
{
	internal class StatsOperation : MemcachedOperationBase
	{
		private readonly MemoryPool<byte> pool;
		private readonly string? type;
		private Dictionary<string, string>? stats;

		public StatsOperation(MemoryPool<byte> pool, string? type = null)
		{
			this.pool = pool;
			this.type = type;
		}

		public IReadOnlyDictionary<string, string> Stats => stats ?? throw new InvalidOperationException("Stats cannot be read until the operation is executed");

		protected override IMemcachedRequest CreateRequest()
		{
			using var builder = new BinaryRequestBuilder(pool, OpCode.Stat);

			if (!String.IsNullOrEmpty(type))
			{
				using var data = pool.Rent(type.Length);
				var span = data.Memory.Span;
				var count = Encoding.ASCII.GetBytes(type.AsSpan(), span);

				builder.SetKey(span.Take(count));
			}

			return builder.Build();
		}

		protected override bool ParseResult(BinaryResponse? response)
		{
			if (stats == null)
				stats = new Dictionary<string, string>();

			Debug.Assert(response != null);

			// if empty key (last response packet)
			// or if error
			// return the response object to break the loop and let the node process the next op.
			if (response.KeyLength != 0 && response.Success)
			{
				// decode stat key/value
				// both are ASCII in memcached
				var key = Encoding.ASCII.GetString(response.Key);
				stats[key] = Encoding.ASCII.GetString(response.Value);

				return true; // we expect more response packets
			}

			return false;
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
