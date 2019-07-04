using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;

namespace Enyim.Caching.Memcached.Operations
{
	internal class MutateOperation : BinaryItemOperation, ICanBeSilent
	{
		public MutateOperation(MemoryPool<byte> allocator, in ReadOnlyMemory<byte> key, MutationMode mode, ulong delta, ulong defaultValue)
			: base(allocator, key)
		{
			Mode = mode;
			Delta = delta;
			DefaultValue = defaultValue;
		}

		public MutationMode Mode { get; }
		public ulong DefaultValue { get; }
		public ulong Delta { get; }
		public Expiration Expiration { get; set; }

		public bool Silent { get; set; }

		public ulong ResultValue { get; set; }

		/*

		Request:

		MUST have extras.
		MUST have key.
		MUST NOT have value.
		Extra data for incr/decr:


			 Byte/     0       |       1       |       2       |       3       |
				/              |               |               |               |
			   |0 1 2 3 4 5 6 7|0 1 2 3 4 5 6 7|0 1 2 3 4 5 6 7|0 1 2 3 4 5 6 7|
			   +---------------+---------------+---------------+---------------+
			  0| Amount to add / subtract                                      |
			   |                                                               |
			   +---------------+---------------+---------------+---------------+
			  8| Initial value                                                 |
			   |                                                               |
			   +---------------+---------------+---------------+---------------+
			 16| Expiration                                                    |
			   +---------------+---------------+---------------+---------------+
			   Total 20 bytes

		 */
		protected override IMemcachedRequest CreateRequest()
		{
			using var builder = new BinaryRequestBuilder(Allocator, Silent ? Protocol.ToSilent((OpCode)Mode) : (OpCode)Mode, 20)
			{
				Cas = Cas
			};

			// store the extra values
			var extra = builder.GetExtra();

			BinaryPrimitives.WriteUInt64BigEndian(extra, Delta);
			BinaryPrimitives.WriteUInt64BigEndian(extra.Slice(8), DefaultValue);
			BinaryPrimitives.WriteUInt32BigEndian(extra.Slice(16), Expiration.Value);

			builder.SetKey(Key);

			return builder.Build();
		}

		/*

			Response:

			MUST NOT have extras.
			MUST NOT have key.
			MUST have value.

				 Byte/     0       |       1       |       2       |       3       |
					/              |               |               |               |
				   |0 1 2 3 4 5 6 7|0 1 2 3 4 5 6 7|0 1 2 3 4 5 6 7|0 1 2 3 4 5 6 7|
				   +---------------+---------------+---------------+---------------+
				  0| 64-bit unsigned response.                                     |
				   |                                                               |
				   +---------------+---------------+---------------+---------------+
				   Total 8 bytes

		*/
		protected override bool ParseResult(BinaryResponse? response)
		{
			if (response == null)
			{
				StatusCode = Protocol.Status.Success;
			}
			else if (response.Success)
			{
				response.MustHave(8, value: true);

				var value = response.Value;
				if (value.Length != 8) throw new IOException($"Expected an 8 byte long value, got {value.Length}");

				ResultValue = BinaryPrimitives.ReadUInt64BigEndian(value);
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
