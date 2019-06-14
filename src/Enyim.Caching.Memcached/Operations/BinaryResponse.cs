using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Text;

namespace Enyim.Caching.Memcached.Operations
{
	internal sealed class BinaryResponse : IResponse
	{
		private const int STATE_INIT = 0;
		private const int STATE_READ_HEADER = 1;
		private const int STATE_READ_BODY = 2;
		private const int STATE_DONE = 3;

		private readonly MemoryPool<byte> pool;
		private int state;
		private int dataReadOffset;

		private IMemoryOwner<byte>? headerOwned;
		private IMemoryOwner<byte>? bodyOwned;

		private Memory<byte> header;
		private Memory<byte> body;
		private Memory<byte> extraBuffer; // this will be sliced from body
		private Memory<byte> keyBuffer; // this will be sliced from body
		private Memory<byte> valueBuffer; // this will be sliced from body

		private string? responseMessage;

		internal BinaryResponse(MemoryPool<byte> pool)
		{
			this.pool = pool;
			StatusCode = -1;
		}

		public byte OpCode { get; private set; }
		public ushort KeyLength { get; private set; }
		public byte DataType { get; private set; }
		public int StatusCode { get; private set; }

		public uint CorrelationId { get; private set; }
		public ulong CAS { get; private set; }

		public ReadOnlySpan<byte> Extra => extraBuffer.Span;
		public ReadOnlySpan<byte> Key => keyBuffer.Span;
		public ReadOnlySpan<byte> Value => valueBuffer.Span;

		public bool Success => StatusCode == Protocol.Status.Success;

		public IMemoryOwner<byte> CloneValue()
		{
			if (bodyOwned == null)
				throw new InvalidOperationException("Cannot clone data: response has no body");

			// we're transferring the ownershop of the body by "soft-cloning" it
			// valueBuffer is only a segment of bodyOwned, and will be released
			// when disposing the ClonedValue (which will dispose bodyOwned)
			var retval = new CloneWrapper<byte>(bodyOwned, valueBuffer);

			// make it null, so our Dispose will not release it
			bodyOwned = null;

			return retval;
		}

		[Conditional("DEBUG")]
		public void MustBeEmpty() => MustHave(0, false, false, false);

		[Conditional("DEBUG")]
		public void MustHave(int? bodyLength, bool? extra = false, bool? key = false, bool? value = false)
		{
			if (StatusCode == Protocol.Status.Success)
			{
				if (bodyLength != null && bodyLength != body.Length) Debug.Fail($"Response expected to have {bodyLength} long body, got {body.Length} instead");

				if (extra != null)
				{
					if (extra.Value && Extra.Length == 0) Debug.Fail("Response must have Extra");
					if (!extra.Value && Extra.Length != 0) Debug.Fail("Response must not have Extra");
				}

				if (key != null)
				{
					if (key.Value && Key.Length == 0) Debug.Fail("Response must have Key");
					if (!key.Value && Key.Length != 0) Debug.Fail("Response must not have Key");
				}

				if (value != null)
				{
					if (value.Value && Value.Length == 0) Debug.Fail("Response must have Value");
					if (!value.Value && Value.Length != 0) Debug.Fail("Response must not have Value");
				}
			}
		}

		private class CloneWrapper<T> : IMemoryOwner<T>
		{
			private IMemoryOwner<T>? owner;

			public CloneWrapper(IMemoryOwner<T> owner, Memory<T> memory)
			{
				this.owner = owner;
				Memory = memory;
			}

			public Memory<T> Memory { get; private set; }

			public void Dispose()
			{
				if (owner != null)
				{
					owner.Dispose();
					Memory = Memory<T>.Empty;

					owner = null;
				}
			}
		}

		public void Dispose()
		{
			// should be diposed already (unless parsing have failed)
			headerOwned?.Dispose();
			bodyOwned?.Dispose();

			headerOwned = null;
			bodyOwned = null;

			header = default;
			body = default;
			extraBuffer = default;
			valueBuffer = default;
			keyBuffer = default;
		}

		public string GetStatusMessage()
		{
			return Value.IsEmpty
					? ""
					: (responseMessage ?? (responseMessage = Encoding.ASCII.GetString(Value)));
		}

		bool IResponse.Read(ReadBuffer stream)
		{
			Span<byte> start;

			switch (state)
			{
				case STATE_INIT:

					headerOwned = pool.Rent(Protocol.HeaderLength);
					// make it exactly HeaderLength long for bound checks
					header = headerOwned.Memory.Take(Protocol.HeaderLength);

					state = STATE_READ_HEADER;
					goto case STATE_READ_HEADER;

				case STATE_READ_HEADER:

					Debug.Assert(headerOwned != null);

					start = header.Span;
					dataReadOffset += stream.CopyTo(start.Slice(dataReadOffset));
					Debug.Assert(dataReadOffset <= Protocol.HeaderLength);

					if (dataReadOffset < Protocol.HeaderLength) return true; // continue reading
					Debug.Assert(dataReadOffset == Protocol.HeaderLength);

					try
					{
						if (!ProcessHeader(start))
							goto case STATE_DONE; // response has no body
					}
					finally
					{
						headerOwned.Dispose();
						headerOwned = null;
					}

					dataReadOffset = 0;
					state = STATE_READ_BODY;
					goto case STATE_READ_BODY;

				case STATE_READ_BODY:

					start = body.Span;
					dataReadOffset += stream.CopyTo(start.Slice(dataReadOffset));
					Debug.Assert(dataReadOffset <= start.Length);

					if (dataReadOffset < start.Length) return true; // not done yet
					Debug.Assert(dataReadOffset == start.Length);

					goto case STATE_DONE;

				case STATE_DONE:
					state = STATE_DONE;
					break;
			}

			return false;
		}

		/*

			 Byte/     0       |       1       |       2       |       3       |
				/              |               |               |               |
			   |0 1 2 3 4 5 6 7|0 1 2 3 4 5 6 7|0 1 2 3 4 5 6 7|0 1 2 3 4 5 6 7|
			   +---------------+---------------+---------------+---------------+
			  0| 0x81          | 0x00          | 0x00          | 0x05          |
			   +---------------+---------------+---------------+---------------+
			  4| 0x04          | 0x00          | 0x00          | 0x00          |
			   +---------------+---------------+---------------+---------------+
			  8| 0x00          | 0x00          | 0x00          | 0x09          |
			   +---------------+---------------+---------------+---------------+
			 12| 0x00          | 0x00          | 0x00          | 0x00          |
			   +---------------+---------------+---------------+---------------+
			 16| 0x00          | 0x00          | 0x00          | 0x00          |
			   +---------------+---------------+---------------+---------------+
			 20| 0x00          | 0x00          | 0x00          | 0x01          |
			   +---------------+---------------+---------------+---------------+
			 24| 0xde          | 0xad          | 0xbe          | 0xef          |
			   +---------------+---------------+---------------+---------------+
			 28| 0x48 ('H')    | 0x65 ('e')    | 0x6c ('l')    | 0x6c ('l')    |
			   +---------------+---------------+---------------+---------------+
			 32| 0x6f ('o')    | 0x57 ('W')    | 0x6f ('o')    | 0x72 ('r')    |
			   +---------------+---------------+---------------+---------------+
			 36| 0x6c ('l')    | 0x64 ('d')    |
			   +---------------+---------------+

			   Total 38 bytes (24 byte header, 4 byte extras, 5 byte key
							   and 5 byte value)


			Field        (offset) (value)
			Magic        (0)    : 0x81
			Opcode       (1)    : 0x00
			Key length   (2,3)  : 0x0005
			Extra length (4)    : 0x04
			Data type    (5)    : 0x00
			Status       (6,7)  : 0x0000
			Total body   (8-11) : 0x0000000E
			Opaque       (12-15): 0x00000000
			CAS          (16-23): 0x0000000000000001
			Extras              :
			  Flags      (24-27): 0xdeadbeef
			Key          (28-32): The textual string: "Hello"
			Value        (33-37): The textual string: "World"

		*/
		/// <summary>
		/// Returns true if further IO is pending. (i.e. body must be read)
		/// </summary>
		/// <param name="header"></param>
		/// <param name="bodyLength"></param>
		/// <param name="extraLength"></param>
		/// <returns></returns>
		private bool ProcessHeader(in ReadOnlySpan<byte> buffer)
		{
			Debug.Assert(buffer.Length >= Protocol.HeaderLength);

			if (buffer[Protocol.Response.HEADER_INDEX_MAGIC] != Protocol.ResponseMagic)
				throw new InvalidOperationException($"Expected magic value {Protocol.ResponseMagic}, received: {buffer[Protocol.Response.HEADER_INDEX_MAGIC]}");

			OpCode = buffer[Protocol.Response.HEADER_INDEX_OPCODE];
			KeyLength = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(Protocol.Response.HEADER_INDEX_KEY));
			DataType = buffer[Protocol.Response.HEADER_INDEX_DATATYPE];
			StatusCode = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(Protocol.Response.HEADER_INDEX_STATUS));
			CorrelationId = BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(Protocol.Response.HEADER_INDEX_OPAQUE));
			CAS = BinaryPrimitives.ReadUInt64BigEndian(buffer.Slice(Protocol.Response.HEADER_INDEX_CAS));

			// uint -> int!! we do not care about extra big data; esp. that memcached limits item size to 1M (by default)
			var bodyLength = (int)BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(Protocol.Response.HEADER_INDEX_BODY_LENGTH));
			var extraLength = buffer[Protocol.Response.HEADER_INDEX_EXTRA];

			if (bodyLength > 0)
			{
				// body/bodyOwned (might be longer than requested)
				// BBBBBBBBBBBBBBBBBBBBbbbb
				// EEEEEEKKKKKKVVVVVVVV
				// |____||    ||      |
				// extraBuffer||      |
				//       |____||      |
				//     keyBuffer      |
				//             |______|
				//               valueBuffer
				bodyOwned = pool.Rent(bodyLength);

				body = bodyOwned.Memory.Take(bodyLength);
				extraBuffer = body.Take(extraLength);
				keyBuffer = body.Slice(extraLength, KeyLength);
				valueBuffer = body.Slice(extraLength + KeyLength);

				return true; // needs more IO
			}

			return false; // all read
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
