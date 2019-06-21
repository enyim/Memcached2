using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;

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
	internal class BinaryRequestBuilder : IDisposable
	{
		private readonly byte operation;
		private readonly byte extraLength;
		private SequenceBuilder? bodyBuilder;

		private Memory<byte> extra;
		private ushort keyLength;

#if DEBUG
		private const int DID_BODY = 3;
		private int whatDidYouDo;
#endif

		public BinaryRequestBuilder(MemoryPool<byte> pool, OpCode operation, byte extraLength = 0) : this(pool, (byte)operation, extraLength) { }

		protected BinaryRequestBuilder(MemoryPool<byte> pool, byte operation, byte extraLength)
		{
			Debug.Assert(extraLength < Protocol.MaxExtraLength);

			CorrelationId = (uint)CorrelationIdGenerator.GetNext(); // request id

			this.operation = operation;
			this.extraLength = extraLength;
			bodyBuilder = new SequenceBuilder(pool);
		}

		public readonly uint CorrelationId;

		public ulong Cas;
#pragma warning disable CS0649
		public readonly ushort Reserved; // field kept for completeness sake
#pragma warning enable CS0649

		private void AllocateExtra()
		{
#if DEBUG
			if (whatDidYouDo == DID_BODY) throw new InvalidOperationException("Extra must be allocated before accessing the body");
#endif
			Debug.Assert(bodyBuilder != null);
			if (extra.IsEmpty && extraLength > 0)
				extra = bodyBuilder.Request(extraLength);
		}

		/// <summary>
		/// Order: SetKey, GetExtra, GetBody
		/// </summary>
		public Span<byte> GetExtra()
		{
			AllocateExtra();
			return extra.Span;
		}

		/// <summary>
		/// Order: SetKey, GetExtra, GetBody
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void SetKey(in ReadOnlyMemory<byte> key) => SetKey(key.Span);

		/// <summary>
		/// Order: SetKey, GetExtra, GetBody
		/// </summary>
		public void SetKey(in ReadOnlySpan<byte> key)
		{
			if (key.Length > Protocol.MaxKeyLength)
				throw new ArgumentException($"Key is too long; was {key.Length}, maximum is {Protocol.MaxKeyLength}");

			AllocateExtra();
			keyLength = (ushort)key.Length;

			if (keyLength > 0)
			{
				Debug.Assert(bodyBuilder != null);
				var tmp = bodyBuilder.Request(key.Length);
				key.CopyTo(tmp.Span);
			}
		}

		/// <summary>
		/// Order: SetKey, GetExtra, GetBody
		/// </summary>
		public SequenceBuilder GetBody()
		{
#if DEBUG
			if (whatDidYouDo >= DID_BODY) throw new InvalidOperationException("body can only be accessed once");
			whatDidYouDo = DID_BODY;
#endif
			Debug.Assert(bodyBuilder != null);

			return bodyBuilder;
		}

		public void Dispose()
		{
			// only clean up if if has not been committed yet
			bodyBuilder?.Dispose();
			bodyBuilder = default;
		}

		public IMemcachedRequest Build()
		{
			Debug.Assert(bodyBuilder != null);

			try
			{
				var retval = new TheBinaryRequest(this, bodyBuilder);
				bodyBuilder = default; // ownership was transferred to the request

				return retval;
			}
			catch (Exception e)
			{
				if (bodyBuilder != default) bodyBuilder.Dispose();

				throw new InvalidOperationException("Fatal exception: could not build the binary request. See the inner exception for details.", e);
			}
		}

		#region [ TheBinaryRequest             ]

		private class TheBinaryRequest : IMemcachedRequest
		{
			private const int STATE_WRITE_HEADER = 0;
			private const int STATE_WRITE_BODY = 1;
			private const int STATE_DONE = 2;

			private readonly BinaryRequestBuilder requestBuilder;

			private readonly SequenceBuilder bodyBuilder;
			private ReadOnlySequence<byte> finalBody;

			private int state;
			private int writeOffset;
			private SequenceCopier? bodyCopier;

#if DEBUG
			private bool disposed;
#endif

			public TheBinaryRequest(BinaryRequestBuilder requestBuilder, SequenceBuilder bodyBuilder)
			{
				this.requestBuilder = requestBuilder;
				this.bodyBuilder = bodyBuilder;
				finalBody = bodyBuilder.Commit();
			}

			public uint CorrelationId => requestBuilder.CorrelationId;

			// will be called by the Node
			public void Dispose()
			{
#if DEBUG
				Debug.Assert(!disposed, "Double dispose at TheBinaryRequest");
				disposed = true;
#endif

				bodyBuilder.Dispose();
				finalBody = default;
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
			public bool WriteTo(WriteBuffer buffer)
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
						if (!finalBody.IsSingleSegment)
							bodyCopier = new SequenceCopier(finalBody, buffer);

						state = STATE_WRITE_BODY;
						goto case STATE_WRITE_BODY;

					case STATE_WRITE_BODY:

						// body is only one span, write it until it's gone
						if (finalBody.IsSingleSegment)
						{
							writeOffset += buffer.TryAppend(finalBody.First.Slice(writeOffset).Span);
							if (writeOffset < finalBody.First.Length) return true;

							Debug.Assert(writeOffset == finalBody.First.Length);
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

				bodyBuilder.Dispose();
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
				target[Protocol.Request.HEADER_INDEX_OPCODE] = requestBuilder.operation;

				BinaryPrimitives.WriteUInt16BigEndian(target.Slice(Protocol.Request.HEADER_INDEX_KEY), requestBuilder.keyLength);

				var extraLength = (byte)requestBuilder.extra.Length;
				target[Protocol.Request.HEADER_INDEX_EXTRA] = extraLength;

				// 5 -- data type, 0 (RAW)
				target[0x05] = 0;
				// TODO 6,7 -- reserved, always 0 (in memcached, others are free to reuse it)
				BinaryPrimitives.WriteUInt16BigEndian(target.Slice(Protocol.Request.HEADER_INDEX_VBUCKET), requestBuilder.Reserved);

				// total payload size
				BinaryPrimitives.WriteUInt32BigEndian(target.Slice(Protocol.Request.HEADER_INDEX_BODY_LENGTH), (uint)finalBody.Length);
				BinaryPrimitives.WriteUInt32BigEndian(target.Slice(Protocol.Request.HEADER_INDEX_OPAQUE), requestBuilder.CorrelationId);
				BinaryPrimitives.WriteUInt64BigEndian(target.Slice(Protocol.Request.HEADER_INDEX_CAS), requestBuilder.Cas);
			}
		}

		#endregion
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
