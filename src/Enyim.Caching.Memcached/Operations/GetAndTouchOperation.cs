using System;
using System.Buffers;
using System.Buffers.Binary;

namespace Enyim.Caching.Memcached.Operations
{
	internal class GetAndTouchOperation : GetOperation
	{
		public GetAndTouchOperation(MemoryPool<byte> allocator, in ReadOnlyMemory<byte> key) : base(allocator, key) { }

		public uint Expires { get; set; }

		/*

			Request:

			MUST have extras.
			MUST have key.
			MUST NOT have value.
			Extra data for touch/gat:

			  Byte/     0       |       1       |       2       |       3       |
				 /              |               |               |               |
				|0 1 2 3 4 5 6 7|0 1 2 3 4 5 6 7|0 1 2 3 4 5 6 7|0 1 2 3 4 5 6 7|
				+---------------+---------------+---------------+---------------+
			   0| Expiration                                                    |
				+---------------+---------------+---------------+---------------+
				Total 4 bytes

		*/
		protected override IMemcachedRequest CreateRequest()
		{
			using var builder = new BinaryRequestBuilder(Pool, Silent ? OpCode.GATQ : OpCode.GAT, 4)
			{
				Cas = Cas
			};

			BinaryPrimitives.WriteUInt32BigEndian(builder.GetExtra(), Expires);
			builder.SetKey(Key);

			return builder.Build();
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
