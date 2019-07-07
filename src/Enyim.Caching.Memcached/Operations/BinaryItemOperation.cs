using System;
using System.Buffers;

namespace Enyim.Caching.Memcached.Operations
{
	internal abstract class BinaryItemOperation : MemcachedOperationBase, IItemOperation
	{
		protected readonly BinaryRequestBuilder Request;

		protected BinaryItemOperation(MemoryPool<byte> allocator, string key, IKeyFormatter keyFormatter, byte extraLength = 0)
		{
			Allocator = allocator;

			try
			{
				Request = new BinaryRequestBuilder(allocator, extraLength);
				Key = Request.SetKey(keyFormatter, key);
			}
			catch
			{
				Request?.Dispose();
				throw;
			}
		}

		protected MemoryPool<byte> Allocator { get; }
		public ReadOnlyMemory<byte> Key { get; }

		protected override IMemcachedRequest CreateRequest() => Request;
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
