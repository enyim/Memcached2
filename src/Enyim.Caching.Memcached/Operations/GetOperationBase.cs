using System;
using System.Buffers;
using System.Buffers.Binary;

namespace Enyim.Caching.Memcached.Operations
{
	internal abstract class GetOperationBase : BinaryItemOperation, ICanBeSilent
	{
		protected GetOperationBase(MemoryPool<byte> allocator, string key, IKeyFormatter keyFormatter, byte extraLength = 0)
			: base(allocator, key, keyFormatter, extraLength) { }

		public bool Silent { get; set; }

		public uint ResultFlags { get; private set; }
		public IMemoryOwner<byte> ResultData { get; private set; } = OwnedMemory<byte>.Empty;

		/*

			Response (if found):

			MUST have extras.
			MAY have key.
			MAY have value.

		*/
		protected override bool ParseResult(BinaryResponse? response)
		{
			if (response == null)
			{
				StatusCode = Protocol.Status.KeyNotFound;
			}
			else if (response.StatusCode == Protocol.Status.Success)
			{
				response.MustHave(null, extra: true, key: null, value: null);

				ResultFlags = BinaryPrimitives.ReadUInt32BigEndian(response.Extra);
				ResultData = response.CloneValue();
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
