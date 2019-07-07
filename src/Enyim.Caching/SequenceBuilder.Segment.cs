using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Enyim.Caching
{
	// based on System.IO.Pipelines.Pipe
	public partial class SequenceBuilder
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

		private class _Segment : ReadOnlySequenceSegment<byte>
		{
			private IMemoryOwner<byte>? memoryOwner;
			private _Segment? next;
			private int end;

			public Memory<byte> AvailableMemory { get; private set; }
			public int Length => End;

			public int End
			{
				get => end;
				set
				{
					end = value;
					Memory = AvailableMemory.Slice(0, end);
				}
			}

			public _Segment? NextSegment
			{
				get => next;
				set
				{
					next = value;
					Next = value;
				}
			}

			public void SetMemory(IMemoryOwner<byte> memoryOwner)
			{
				this.memoryOwner = memoryOwner;
				AvailableMemory = this.memoryOwner.Memory;
				RunningIndex = 0;
				End = 0;
				NextSegment = default;
			}

			public void ReleaseMemory()
			{
				if (memoryOwner != null)
				{
					memoryOwner.Dispose();
					memoryOwner = default;
					AvailableMemory = Memory<byte>.Empty;
				}
			}

			public int WritableBytes
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => AvailableMemory.Length - End;
			}

			public void SetNext(_Segment value)
			{
				NextSegment = value;
				var current = this;

				while (current.next != null)
				{
					current.next.RunningIndex = current.RunningIndex + current.Length;
					current = current.next;
				}
			}
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
