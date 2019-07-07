using System;
using System.Buffers;
using System.Diagnostics;

namespace Enyim.Caching.Memcached.Operations
{
	internal class NoOp : MemcachedOperationBase
	{
		private readonly MemoryPool<byte> allocator;

		public NoOp(MemoryPool<byte> allocator)
		{
			this.allocator = allocator;
		}

		protected override IMemcachedRequest CreateRequest()
		{
			return new BinaryRequestBuilder(allocator, 0)
			{
				Operation = OpCode.NoOp
			};
		}

		protected override bool ParseResult(BinaryResponse? response)
		{
			Debug.Assert(response != null);

			response.MustBeEmpty();
			StatusCode = 0;

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
