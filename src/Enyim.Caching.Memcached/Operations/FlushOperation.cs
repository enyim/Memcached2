using System;
using System.Buffers;
using System.Buffers.Binary;

namespace Enyim.Caching.Memcached.Operations
{
	internal class FlushOperation : MemcachedOperationBase
	{
		private readonly MemoryPool<byte> allocator;

		public FlushOperation(MemoryPool<byte> allocator)
		{
			this.allocator = allocator;
		}

		public Expiration When { get; set; }

		/*
			Request:

			MAY have extras.
			MUST NOT have key.
			MUST NOT have value.
			Extra data for flush:

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
			using var builder = new BinaryRequestBuilder(allocator, OpCode.Flush, When.IsNever ? (byte)0 : (byte)4);

			if (!When.IsNever)
			{
				BinaryPrimitives.WriteUInt32BigEndian(builder.GetExtra(), When.Value);
			}

			return builder.Build();
		}

		/*

			Response:

			MUST NOT have extras.
			MUST NOT have key.
			MUST NOT have value.

		*/
		protected override bool ParseResult(BinaryResponse? response)
		{
			if (response == null)
			{
				StatusCode = Protocol.Status.Success;
			}
			else
			{
				response.MustHave(0);
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
