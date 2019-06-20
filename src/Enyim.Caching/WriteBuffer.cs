using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Enyim.Caching
{
	public sealed class WriteBuffer
	{
		private Memory<byte> buffer;
		private int position;
		private int remaining;

		public WriteBuffer(in Memory<byte> buffer)
		{
			this.buffer = buffer;
			Restart();
		}

		public Span<byte> Span => buffer.Span;
		public bool IsFull => position == buffer.Length;

		/// <summary>
		/// The current position in the buffer
		/// </summary>
		public int Position => position;

		public void Restart()
		{
			position = 0;
			remaining = buffer.Length;
		}

		// returns the amount appended
		public int TryAppend(in ReadOnlySpan<byte> data)
		{
			Debug.Assert(remaining >= 0);

			if (remaining == 0 || data.IsEmpty) return 0;
			var toWrite = data.Length > remaining
							? data.Slice(0, remaining)
							: data;

			toWrite.CopyTo(buffer.Span.Slice(position));
			AdvanceCore(toWrite.Length);

			return toWrite.Length;
		}

		public Memory<byte> Slice() => buffer.Slice(0, position);

		public Span<byte> Want(int count)
		{
			if (remaining < count) return Span<byte>.Empty;

			var retval = buffer.Span.Slice(position, count);
			AdvanceCore(count);

			return retval;
		}

		public void Advance(int amount)
		{
			if (amount < 1)
				throw new ArgumentOutOfRangeException(nameof(amount), nameof(amount) + "must be positive");

			AdvanceCore(amount);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void AdvanceCore(int amount)
		{
			position += amount;
			remaining -= amount;

			Debug.Assert(position <= buffer.Length);
			Debug.Assert(remaining >= 0);
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
