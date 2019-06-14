using System;
using System.Diagnostics;

namespace Enyim.Caching
{
	public sealed class ReadBuffer
	{
		private ReadOnlyMemory<byte> buffer;

		private int remaining;
		private int position;

		internal ReadBuffer() { }

		internal void Initialize(in ReadOnlyMemory<byte> buffer)
		{
			this.buffer = buffer;
			remaining = 0;
			position = 0;
		}

		public int Remaining => remaining;
		public bool IsEmpty => remaining == 0;

		public ReadOnlySpan<byte> Span => buffer.Span.Slice(position, remaining);

		/// <summary>
		/// Reads a sequence of bytes
		/// </summary>
		/// <param name="buffer">
		///    An array of bytes. When this method returns, the buffer contains the specified
		///    byte array with the values between offset and (offset + count - 1) replaced by
		///    the bytes read from the current source.</param>
		/// <returns>The total number of bytes read into the target. This can be less than the number of bytes requested if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.</returns>
		public int CopyTo(in Span<byte> target)
		{
			Debug.Assert(remaining >= 0);

			var toRead = Math.Min(remaining, target.Length);
			if (toRead == 0) return 0;

			buffer.Span
				.Slice(position, toRead)
				.CopyTo(target);

			position += toRead;
			remaining -= toRead;

			return toRead;
		}

		internal void SetDataAvailable(int length)
		{
			Debug.Assert(length <= buffer.Length, "length cannot be larger than buffer.Length");

			position = 0;
			remaining = length;
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
