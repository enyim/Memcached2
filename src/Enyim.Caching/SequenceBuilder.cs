using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Enyim.Caching
{
	// based on System.IO.Pipelines.Pipe
	public partial class SequenceBuilder : IDisposable
	{
		#region [ LICENSE ]
		/*
		The MIT License (MIT)

		Copyright (c) .NET Foundation and Contributors

		All rights reserved.

		Permission is hereby granted, free of charge, to any person obtaining a copy
		of this software and associated documentation files (the "Software"), to deal
		in the Software without restriction, including without limitation the rights
		to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
		copies of the Software, and to permit persons to whom the Software is
		furnished to do so, subject to the following conditions:

		The above copyright notice and this permission notice shall be included in all
		copies or substantial portions of the Software.

		THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
		IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
		FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
		AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
		LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
		OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
		SOFTWARE.
		*/
		#endregion

		private readonly MemoryPool<byte> allocator;
		private int segmentSize;

		private int length;
		private _Segment? start;
		private _Segment? current;
		private bool done;
		private bool disposed;

		public SequenceBuilder(MemoryPool<byte> allocator, int minimumSegmentSize = 32)
		{
			this.allocator = allocator;
			segmentSize = minimumSegmentSize;
		}

		public int Length => length;

		public Memory<byte> Request(int count, bool advance = true)
		{
			if (done) throw new InvalidOperationException("cannot write after commit");
			if (count == 0) return Memory<byte>.Empty;
			if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));

			if (current == null || current.WritableBytes < count)
			{
				var newSegment = new _Segment();
				newSegment.SetMemory(allocator.Rent(CalcSegmentSize(count)));

				current?.SetNext(newSegment);
				current = newSegment;

				if (start == null) start = current;
			}

			var retval = current.AvailableMemory.Slice(current.End, count);
			if (advance) AdvanceCore(count);

			return retval;
		}

		public SequencePosition Mark()
		{
			return new SequencePosition(current, current?.End ?? 0);
		}

		public ReadOnlySequence<byte> Slice(SequencePosition markStart, SequencePosition markEnd)
		{
			if (start == null) return ReadOnlySequence<byte>.Empty;

			var a = (_Segment)(markStart.GetObject() ?? start);
			var b = (_Segment)(markEnd.GetObject() ?? start);

			var pa = markStart.GetInteger();
			var pb = markEnd.GetInteger();

			if (a == b && pa == pb) return ReadOnlySequence<byte>.Empty;

			return new ReadOnlySequence<byte>(a, pa, b, pb);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private int CalcSegmentSize(int requested)
		{
			requested = Math.Max(segmentSize, requested);
			segmentSize = (int)(segmentSize * 1.5);

			return Math.Min(allocator.MaxBufferSize, requested);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Advance(int by)
		{
			if (by == 0) return;
			if (by < 0) throw new ArgumentOutOfRangeException(nameof(by), "Cannot go back");
			if (current == null) throw new InvalidOperationException("Cannot advance without getting memory");

			var buffer = current.AvailableMemory;
			if (current.End > buffer.Length - by)
				throw new ArgumentOutOfRangeException("Cannot advance after the buffer's end");

			AdvanceCore(by);
		}

		public void Append(SequenceBuilder other)
		{
			if (other.disposed)
				throw new ObjectDisposedException(nameof(other), "Cannot append a disposed SequenceBuilder");

			// current instance is empty; just clone the other builder
			if (current == null)
			{
				start = other.start;
				current = other.current;
				segmentSize = other.segmentSize;
				length = other.length;
			}
			else
			{
				// the other one is empty, quit
				var otherSegment = other.start ?? other.current;
				if (otherSegment == null) return; // 'other' is empty, there is nothing to append

				// append the other builder's list to ours
				// our current will be their current
				if (start == null) start = current;

				current.SetNext(otherSegment);

				// find the end of their list
				while (otherSegment != null)
				{
					current = otherSegment;
					otherSegment = otherSegment.NextSegment;
				}

				length += other.length;
				segmentSize = Math.Max(segmentSize, other.segmentSize);
			}

			// the other builder cannot be used anymore
			other.Clear();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void AdvanceCore(int by)
		{
			Debug.Assert(current != null);
			Debug.Assert(current.Next == null);
			Debug.Assert(current.End <= current.AvailableMemory.Length - by);

			current.End += by;
			length += by;
		}

		public ReadOnlySequence<byte> Commit()
		{
			if (disposed) throw new ObjectDisposedException("SequenceBuilder");

			done = true;
			if (length == 0) return ReadOnlySequence<byte>.Empty;

			Debug.Assert(start != null);
			Debug.Assert(current != null);

			return new ReadOnlySequence<byte>(start, 0, current, current.End);
		}

		public void Dispose()
		{
			if (disposed) return;

			// release the allocated memory
			var segment = start ?? current;
			while (segment != null)
			{
				segment.ReleaseMemory();
				segment = segment.NextSegment;
			}

			Clear();
		}

		private void Clear()
		{
			if (disposed) return;

			length = 0;
			start = null;
			current = null;
			done = true;
			disposed = true;
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
