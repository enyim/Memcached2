using System;
using System.Buffers;
using System.Buffers.Binary;

namespace Enyim.Caching.Memcached.Operations
{
	internal class StoreOperation : BinaryItemOperation, ICanBeSilent
	{
		private readonly uint flags;
		private SequenceBuilder? value;

		public StoreOperation(MemoryPool<byte> pool, in ReadOnlyMemory<byte> key, StoreMode mode, uint flags, SequenceBuilder value)
			: base(pool, key)
		{
			Mode = mode;
			this.flags = flags;
			this.value = value;
		}

		public bool Silent { get; set; }

		public StoreMode Mode { get; }
		public Expiration Expiration { get; set; }

		/*

			Request:

			MUST have extras.
			MUST have key.
			MAY have value.
			Extra data for set/add/replace:

				 Byte/     0       |       1       |       2       |       3       |
					/              |               |               |               |
				   |0 1 2 3 4 5 6 7|0 1 2 3 4 5 6 7|0 1 2 3 4 5 6 7|0 1 2 3 4 5 6 7|
				   +---------------+---------------+---------------+---------------+
				  0| Flags                                                         |
				   +---------------+---------------+---------------+---------------+
				  4| Expiration                                                    |
				   +---------------+---------------+---------------+---------------+
				   Total 8 bytes

		*/
		protected override IMemcachedRequest CreateRequest()
		{
			using var builder = new BinaryRequestBuilder(Pool, Silent ? Protocol.ToSilent((OpCode)Mode) : (OpCode)Mode, 8)
			{
				Cas = Cas
			};

			var extra = builder.GetExtra();

			BinaryPrimitives.WriteUInt32BigEndian(extra, flags);
			BinaryPrimitives.WriteUInt32BigEndian(extra.Slice(4), Expiration.Value);

			builder.SetKey(Key);

			if (value != null)
			{
				builder.GetBody().Append(value);
				value = null;
			}

			return builder.Build();
		}

		/*

			Response:

			MUST have CAS
			MUST NOT have extras
			MUST NOT have key
			MUST NOT have value

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
