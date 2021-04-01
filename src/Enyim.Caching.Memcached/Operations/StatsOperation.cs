using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

#if NETSTANDARD2_0
using Caching;
#endif

namespace Enyim.Caching.Memcached.Operations
{
	internal class StatsOperation : MemcachedOperationBase
	{
		private readonly MemoryPool<byte> allocator;
		private readonly string? type;
		private Dictionary<string, string>? stats;

		public StatsOperation(MemoryPool<byte> allocator, string? type = null)
		{
			this.allocator = allocator;
			this.type = type;
		}

		public IReadOnlyDictionary<string, string> Stats => stats ?? throw new InvalidOperationException("Stats cannot be read until the operation is executed");

		protected override IMemcachedRequest CreateRequest()
		{
			var builder = new BinaryRequestBuilder(allocator, 0)
			{
				Operation = OpCode.Stat
			};

			try
			{
				if (!String.IsNullOrEmpty(type))
				{
					using var data = allocator.Rent(type.Length);
					var span = data.Memory.Span;
					var count = Encoding.ASCII.GetBytes(type.AsSpan(), span);

					builder.SetKeyRaw(span.Take(count));
				}

				builder.Commit();
			}
			catch
			{
				builder.Dispose();
				throw;
			}

			return builder;
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
