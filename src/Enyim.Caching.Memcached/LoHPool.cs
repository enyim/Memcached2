using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Enyim.Caching
{
	// purpose of this class is to allocate buffers from LoH to prevent defragmentation
	public class LoHMemoryPool : MemoryPool<byte>
	{
		private readonly int bufferSize;
		private readonly object syncLock;

		private SliceFactory? lastFactory;
		private bool isDisposed;

		public LoHMemoryPool(int maxBufferSize = AsyncSocket.Defaults.MaxBufferSize)
		{
			bufferSize = Math.Max(128 * 1024, maxBufferSize); // make sure we are on LoH

			syncLock = new object();
			lastFactory = new SliceFactory(new byte[bufferSize]);
		}

		public override int MaxBufferSize => bufferSize;

		public override IMemoryOwner<byte> Rent(int minBufferSize)
		{
			if (minBufferSize > bufferSize) throw new ArgumentOutOfRangeException($"Required buffer size {minBufferSize} is larger than the maximum size {bufferSize}");
			if (isDisposed) throw new ObjectDisposedException(nameof(LoHMemoryPool));

			Debug.Assert(lastFactory != null);
			IMemoryOwner<byte> retval;

			lock (syncLock)
			{
				if ((retval = lastFactory.TryRent(minBufferSize)).Memory.IsEmpty)
				{
					lastFactory = new SliceFactory(new byte[bufferSize]);
					retval = lastFactory.TryRent(minBufferSize);

					Debug.Assert(!retval.IsEmpty());
				}
			}

			return retval;
		}

		protected override void Dispose(bool disposing)
		{
			lock (syncLock)
			{
				if (isDisposed) return;

				lastFactory = null;
				isDisposed = true;
			}
		}

		#region [ SliceFactory                 ]

		/// <summary>
		/// SAEAs pin their buffer so to prevent unneccessary fragmentation we allocate a large buffer on LoH and allocate smaller buffers from it.
		/// </summary>
		/// <remarks>
		/// The factory only cares if all slices created by it are returned and will not ever reuse them. This should only be a problem if a lot of sockets are allocated over and over again without destroying them. Which should not happen.
		/// </remarks>
		private class SliceFactory
		{
			private readonly Memory<byte> memory;
			private int offset;
			private int used;

			public SliceFactory(in Memory<byte> memory)
			{
				this.memory = memory;
			}

			/// <summary>
			/// Returns true if no slices have been allocated from this buffer or all slices have been returned
			/// </summary>
			public bool HasReferences => used != 0;

			private class Retval : IMemoryOwner<byte>
			{
				private readonly SliceFactory owner;

				public Retval(SliceFactory owner, in Memory<byte> memory)
				{
					this.owner = owner;
					Memory = memory;
				}

				public Memory<byte> Memory { get; private set; }

				public void Dispose()
				{
					owner.TrackReturn(Memory);
					Memory = Memory<byte>.Empty;
				}
			}

			public IMemoryOwner<byte> TryRent(int requested)
			{
				// round up to multiples of 8
				var toAllocate = (requested + 7) / 8 * 8;
				var newOffset = offset + toAllocate;
				if (newOffset > memory.Length) return OwnedMemory<byte>.Empty; //does not fit

				var retval = new Retval(this, memory.Slice(offset, requested));
				used += requested;
				offset = newOffset;

				return retval;
			}

			private void TrackReturn(in Memory<byte> buffer)
			{
				Debug.Assert(MemoryMarshal.TryGetArray<byte>(buffer, out var other)
								&& MemoryMarshal.TryGetArray<byte>(memory, out var mine)
								&& other.Array == mine.Array, "buffer is not owned by this pool");

				used -= buffer.Length;
				Debug.Assert(used >= 0, "a buffer was returned twice");
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
