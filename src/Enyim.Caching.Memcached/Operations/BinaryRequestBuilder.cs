using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;

namespace Enyim.Caching.Memcached.Operations
{
	/*

	PACKET
	======

	 Byte/     0       |       1       |       2       |       3       |
		/              |               |               |               |
		|0 1 2 3 4 5 6 7|0 1 2 3 4 5 6 7|0 1 2 3 4 5 6 7|0 1 2 3 4 5 6 7|
		+---------------+---------------+---------------+---------------+
		0/ HEADER                                                        /
		/                                                               /
		/                                                               /
		/                                                               /
		+---------------+---------------+---------------+---------------+
		24/ COMMAND-SPECIFIC EXTRAS (as needed)                           /
		+/  (note length in the extras length header field)              /
		+---------------+---------------+---------------+---------------+
		m/ Key (as needed)                                               /
		+/  (note length in key length header field)                     /
		+---------------+---------------+---------------+---------------+
		n/ Value (as needed)                                             /
		+/  (note length is total body length header field, minus        /
		+/   sum of the extras and key length body fields)               /
		+---------------+---------------+---------------+---------------+
		Total 24 bytes
	*/
	internal class BinaryRequestBuilder : IDisposable, IMemcachedRequest
	{
		private const int STATE_WRITE_HEADER = 0;
		private const int STATE_WRITE_BODY = 1;
		private const int STATE_DONE = 2;

		private readonly byte extraLength;
		private readonly SequenceBuilder bodyBuilder;

		private Memory<byte> extra;
		private ushort keyLength;

#if DEBUG
		private bool didBody;
		private bool didKey;
		private bool didExtra;
#endif

		private ReadOnlySequence<byte> finalBody;
		private int state;
		private int writeOffset;
		private SequenceCopier? bodyCopier;
		private ReadOnlyMemory<byte> singleSegmentBody;

		public BinaryRequestBuilder(MemoryPool<byte> allocator, byte extraLength)
		{
			Debug.Assert(extraLength < Protocol.MaxExtraLength);

			this.extraLength = extraLength;
			CorrelationId = (uint)CorrelationIdGenerator.GetNext(); // request id
			bodyBuilder = new SequenceBuilder(allocator, Protocol.HeaderLength + extraLength);
		}

		public OpCode Operation;
		public ulong Cas;
		public uint CorrelationId { get; }

#pragma warning disable CS0649
		public readonly ushort Reserved; // field kept for completeness sake
#pragma warning enable CS0649

		/// <summary>
		/// Order: SetKey, GetExtra, GetBody
		/// </summary>
		private void AllocateExtras()
		{
			if (extra.Length == 0 && extraLength > 0)
				extra = bodyBuilder.Request(extraLength);

#if DEBUG
			didExtra = true;
#endif
		}

		/// <summary>
		/// Order: SetKey, GetExtra, GetBody
		/// </summary>
		public Span<byte> GetExtraBuffer()
		{
#if DEBUG
			Debug.Assert(!didBody, "Extra must be allocated before accessing the body");
#endif
			AllocateExtras();

			return extra.Span;
		}

		public ReadOnlyMemory<byte> SetKey(IKeyFormatter formatter, string key)
		{
#if DEBUG
			Debug.Assert(!didKey, "Key can only be set once");
			Debug.Assert(!didBody, "Key must be set before accessing the body");
			didKey = true;
#endif
			AllocateExtras();

			var start = bodyBuilder.Mark();
			var preKey = bodyBuilder.Length;
			Debug.Assert(preKey == extra.Length, "unexpected data was written between extra & key");

			formatter.Serialize(bodyBuilder, key);

			var end = bodyBuilder.Mark();
			var tmpLength = bodyBuilder.Length - preKey;
			if (tmpLength > Protocol.MaxKeyLength)
				throw new ArgumentException($"Key is too long; was {tmpLength}, maximum is {Protocol.MaxKeyLength}");

			keyLength = (ushort)tmpLength;

			var seq = bodyBuilder.Slice(start, end);

			if (seq.IsSingleSegment)
			{
				Debug.Assert(seq.First.Length == keyLength);
				return seq.First;
			}

			return seq.ToArray();
		}

		public void SetKeyRaw(ReadOnlySpan<byte> key)
		{
#if DEBUG
			Debug.Assert(!didKey, "Key can only be set once");
			Debug.Assert(!didBody, "Key must be set before accessing the body");
			didKey = true;
#endif
			AllocateExtras();

			if (key.Length > Protocol.MaxKeyLength)
				throw new ArgumentException($"Key is too long; was {key.Length}, maximum is {Protocol.MaxKeyLength}");

			keyLength = (ushort)key.Length;
			bodyBuilder.Append(key);
		}

		/// <summary>
		/// Order: GetExtra, SetKey, GetBody
		/// </summary>
		public SequenceBuilder GetBody()
		{
#if DEBUG
			Debug.Assert(extraLength == 0 || didExtra, "Extras must be set before the body");
			Debug.Assert(!didBody, "body can only be accessed once");
			didBody = true;
#endif

			return bodyBuilder;
		}

		public void Dispose()
		{
			// only clean up if if has not been committed yet
			bodyBuilder.Dispose();
		}

		public void Commit()
		{
			try { finalBody = bodyBuilder.Commit(); }
			catch (Exception e)
			{
				bodyBuilder.Dispose();

				throw new InvalidOperationException("Fatal exception: could not build the binary request. See the inner exception for details.", e);
			}
		}

		/*

		PACKET
		======

		 Byte/     0       |       1       |       2       |       3       |
			/              |               |               |               |
			|0 1 2 3 4 5 6 7|0 1 2 3 4 5 6 7|0 1 2 3 4 5 6 7|0 1 2 3 4 5 6 7|
			+---------------+---------------+---------------+---------------+
			0/ HEADER                                                        /
			/                                                               /
			/                                                               /
			/                                                               /
			+---------------+---------------+---------------+---------------+
			24/ COMMAND-SPECIFIC EXTRAS (as needed)                           /
			+/  (note length in the extras length header field)              /
			+---------------+---------------+---------------+---------------+
			m/ Key (as needed)                                               /
			+/  (note length in key length header field)                     /
			+---------------+---------------+---------------+---------------+
			n/ Value (as needed)                                             /
			+/  (note length is total body length header field, minus        /
			+/   sum of the extras and key length body fields)               /
			+---------------+---------------+---------------+---------------+
			Total 24 bytes
		*/
		bool IRequest.WriteTo(WriteBuffer buffer)
		{
			// 0. init header
			// 1. loop on header
			// 2. loop on Key, if any
			// 3. loop on Data, if any
			// 4. done
			switch (state)
			{
				case STATE_WRITE_HEADER:
					// this will either get a Span that can fit the header data or nothing
					// we will not write partial header
					var span = buffer.Want(Protocol.HeaderLength);
					if (span.IsEmpty) return true; // header does not fit into the request buffer

					WriteHeader(span);
					if (finalBody.IsEmpty) break; // no body, quit

					writeOffset = 0;
					if (finalBody.IsSingleSegment)
						singleSegmentBody = finalBody.First;
					else
						bodyCopier = new SequenceCopier(finalBody, buffer);

					state = STATE_WRITE_BODY;
					goto case STATE_WRITE_BODY;

				case STATE_WRITE_BODY:

					// body is only one span, write it until it's gone
					if (!singleSegmentBody.IsEmpty)
					{
						writeOffset += buffer.TryAppend(singleSegmentBody.Slice(writeOffset).Span);
						if (writeOffset < singleSegmentBody.Length) return true;

						Debug.Assert(writeOffset == singleSegmentBody.Length);
					}
					else
					{
						Debug.Assert(bodyCopier != null);
						// body consists of munltiple spans; the SequenceCopier will process it
						if (bodyCopier.Copy()) return true;
						bodyCopier = default;
					}

					break;

				default: throw new InvalidOperationException("undhandled state: " + state); // should not happen
			}

			state = STATE_DONE;

			//bodyBuilder.Dispose();
			finalBody = default;

			return false;
		}

		/*

		HEADER
		======

		  Byte/     0       |       1       |       2       |       3       |
			 /              |               |               |               |
			 |0 1 2 3 4 5 6 7|0 1 2 3 4 5 6 7|0 1 2 3 4 5 6 7|0 1 2 3 4 5 6 7|
			  +---------------+---------------+---------------+---------------+
			0 | Magic         | Opcode        | Key length                    |
			  +---------------+---------------+---------------+---------------+
			4 | Extras length | Data type     | vbucket id                    |
			  +---------------+---------------+---------------+---------------+
			8 | Total body length                                             |
			  +---------------+---------------+---------------+---------------+
			12| Opaque                                                        |
			  +---------------+---------------+---------------+---------------+
			16| CAS                                                           |
			  |                                                               |
			  +---------------+---------------+---------------+---------------+
			  Total 24 bytes
		*/

		private void WriteHeader(Span<byte> target)
		{
			target[Protocol.Request.HEADER_INDEX_MAGIC] = Protocol.RequestMagic; // magic
			target[Protocol.Request.HEADER_INDEX_OPCODE] = (byte)Operation;

			BinaryPrimitives.WriteUInt16BigEndian(target.Slice(Protocol.Request.HEADER_INDEX_KEY), keyLength);

			target[Protocol.Request.HEADER_INDEX_EXTRA] = (byte)extra.Length;

			// 5 -- data type, 0 (RAW)
			target[0x05] = 0;
			// TODO 6,7 -- reserved, always 0 (in memcached, others are free to reuse it)
			BinaryPrimitives.WriteUInt16BigEndian(target.Slice(Protocol.Request.HEADER_INDEX_VBUCKET), Reserved);

			// total payload size
			BinaryPrimitives.WriteUInt32BigEndian(target.Slice(Protocol.Request.HEADER_INDEX_BODY_LENGTH), (uint)finalBody.Length);
			BinaryPrimitives.WriteUInt32BigEndian(target.Slice(Protocol.Request.HEADER_INDEX_OPAQUE), CorrelationId);
			BinaryPrimitives.WriteUInt64BigEndian(target.Slice(Protocol.Request.HEADER_INDEX_CAS), Cas);
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
