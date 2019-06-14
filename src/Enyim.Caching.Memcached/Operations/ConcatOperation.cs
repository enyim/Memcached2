using System;
using System.Buffers;

namespace Enyim.Caching.Memcached.Operations
{
	internal class ConcatOperation : BinaryItemOperation, ICanBeSilent
	{
		public ConcatOperation(MemoryPool<byte> pool, in ReadOnlyMemory<byte> key, ConcatenationMode mode, in ReadOnlyMemory<byte> data)
			: base(pool, key)
		{
			Mode = mode;
			Data = data;
		}

		public ConcatenationMode Mode { get; }
		public ReadOnlyMemory<byte> Data { get; }

		public bool Silent { get; set; }

		/*

			Request:

			MUST NOT have extras.
			MUST have key.
			MUST have value.

		*/
		protected override IMemcachedRequest CreateRequest()
		{
			using var builder = new BinaryRequestBuilder(Pool, Silent ? Protocol.ToSilent((OpCode)Mode) : (OpCode)Mode)
			{
				Cas = Cas
			};

			builder.SetKey(Key);
			builder.GetBody().Append(Data);

			return builder.Build();
		}

		/*

			Response:

			MUST NOT have extras.
			MUST NOT have key.
			MUST NOT have value.
			MUST have CAS

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
