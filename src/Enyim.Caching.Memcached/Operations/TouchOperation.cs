using System;
using System.Buffers;
using System.Buffers.Binary;

namespace Enyim.Caching.Memcached.Operations
{
	internal class TouchOperation : BinaryItemOperation
	{
		public TouchOperation(MemoryPool<byte> allocator, string key, IKeyFormatter keyFormatter)
			: base(allocator, key, keyFormatter, 4) { }

		public Expiration Expiration { get; set; }

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

			returns the flags if nonzero (in 1.5.x)

		*/
		public void Initialize()
		{
			try
			{
				Request.Operation = OpCode.Touch;
				Request.Cas = Cas;

				BinaryPrimitives.WriteUInt32BigEndian(Request.GetExtraBuffer(), Expiration.Value);

				Request.Commit();
			}
			catch
			{
				Request?.Dispose();
				throw;
			}
		}

		protected override bool ParseResult(BinaryResponse? response)
		{
			if (response == null)
			{
				StatusCode = Protocol.Status.KeyNotFound;
			}
			else
			{
#if DEBUG
				if (response.Body.Length == 0)
					response.MustBeEmpty();
				else
					response.MustHave(4, true, false, false);
#endif
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
