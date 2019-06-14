//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Net.Sockets;
//using System.Runtime.InteropServices;
//using System.Threading;

//namespace Enyim.Caching
//{
//	/// <summary>
//	/// Provides an efficient way of creating SocketAsyncEventArgs that are using pinned buffers.
//	/// It assumes that the returned SocketAsyncEventArgs are long-lived and not allocated frequently.
//	/// (Which is true for the AsyncSocket/ClusterBase.)
//	/// </summary>
//	internal sealed class SocketAsyncEventArgsFactory : IDisposable
//	{
//		internal static readonly SocketAsyncEventArgsFactory Instance = new SocketAsyncEventArgsFactory(Math.Max(100 * 1024, AsyncSocket.Defaults.MaxBufferSize)); // we want LOH either way

//		private readonly object UpdateLock;
//		private readonly int chunkSize;

//		// list of factories; the empty one is always at the top
//		// a factory can only provide buffers up to its maximum size
//		// so if a factory runs out of space, we have to create a new one
//		//private readonly ConcurrentStack<BufferFactory> factories;
//		// tracks which SAEA's inner buffer belongs to which factory
//		private readonly ConcurrentDictionary<SocketAsyncEventArgs, (BufferFactory owner, Memory<byte> buffer)> knownEventArgs;

//		private BufferFactory lastFactory;

//		private SocketAsyncEventArgsFactory(int chunkSize)
//		{
//			this.chunkSize = chunkSize;

//			UpdateLock = new object();
//			//factories = new ConcurrentStack<BufferFactory>();
//			lastFactory = new BufferFactory(chunkSize);
//			knownEventArgs = new ConcurrentDictionary<SocketAsyncEventArgs, (BufferFactory owner, Memory<byte> buffer)>();
//		}

//		internal SocketAsyncEventArgs Take(int size)
//		{
//			if (size > chunkSize)
//				throw new ArgumentOutOfRangeException($"Required buffer size {size} is larger than the chunk size {chunkSize}");

//			BufferFactory bufferFactory;
//			Memory<byte> segment;
//			bool success;


//			while (!lastFactory.TryAlloc(size, out segment))
//			{
//				lock (UpdateLock)
//				{
//					if (!lastFactory.TryAlloc(size, out segment))
//						lastFactory = new BufferFactory(chunkSize);
//				}
//			}

//			// allocate new pools until we can acquire a buffer with the required size
//			// The way SAEAs and buffers are used we're fine with continously allocating
//			// new BufferFactories when we run out of space (and not being able to reuse them)
//			//while (!factories.TryPeek(out bufferFactory) || !bufferFactory.TryAlloc(size, out segment))
//			//{
//			//	lock (UpdateLock)
//			//	{
//			//		if (!factories.TryPeek(out bufferFactory) || !bufferFactory.TryAlloc(size, out segment))
//			//		{
//			//			// create & store a new factory, allocation will be handled by the while loop
//			//			factories.Push(new BufferFactory(chunkSize));
//			//		}
//			//	}
//			//}

//			var retval = new SocketAsyncEventArgs();
//			retval.SetBuffer(segment);

//			success = knownEventArgs.TryAdd(retval, (bufferFactory, segment));
//			Debug.Assert(success);

//			return retval;
//		}

//		internal void Return(SocketAsyncEventArgs eventArgs)
//		{
//			if (!knownEventArgs.TryRemove(eventArgs, out var entry))
//				throw new InvalidOperationException("Unknown SocketAsyncEventArgs, could not map it to a BufferFactory.");

//			entry.owner.Forget(entry.buffer);
//		}

//		/// <summary>
//		/// Compacts the buffer list by removing and destroying all empty BufferFactories.
//		/// </summary>
//		internal void Compact()
//		{
////			lock (UpdateLock)
////			{
////				if (factories.Count == 0) return;

////				var all = new BufferFactory[factories.Count];
////				var didPop = factories.TryPopRange(all);

////				// dispose all empty entries
////				// and put the rest back into the stack
////				for (var i = didPop - 1; i >= 0; i--)
////				{
////					var current = all[i];

////					if (current == null) continue;
////					if (!current.IsEmpty)
////					{
////						factories.Push(current);
////					}
////					#region debug checks
////#if DEBUG
////					else
////					{
////						foreach (var tmp in knownEventArgs.Values)
////						{
////							if (tmp.Item1 == current)
////								throw new InvalidOperationException("Factory is still mapped to a SocketAsyncEventArgs");
////						}
////					}
////#endif
////					#endregion
////				}
////			}
//		}

//		public void Dispose()
//		{
//			lock (UpdateLock)
//			{
//				if (factories == null) return;

//				foreach (var tmp in knownEventArgs.Keys)
//				{
//					try { tmp.Dispose(); }
//					catch { }
//				}

//				knownEventArgs.Clear();
//				factories.Clear();
//			}
//		}

//		#region [ BufferFactory                ]

//		/// <summary>
//		/// Implements a buffer factory. Returns ArraySegments using a pinned byte array as underlying storage.
//		/// </summary>
//		/// <remarks>
//		/// The BufferFactory only cares if all buffers created by it are returned without being able to reuse them.
//		/// (SAEAFactory will clean up the empty BufferFactories during compaction.)
//		/// </remarks>
//		private class BufferFactory
//		{
//			private byte[] data;
//			private int offset;
//			private int usage;

//			public BufferFactory(int size)
//			{
//				data = new byte[size];
//			}

//			public bool IsEmpty => Interlocked.CompareExchange(ref usage, 0, 0) == 0;

//			public bool TryAlloc(int size, out Memory<byte> buffer)
//			{
//				// round up to multiples of 8
//				size = (size + 7) / 8 * 8;

//				// repeat until we manage to allocate the buffer or we run out of space
//				while (true)
//				{
//					var oldOffset = offset;
//					var newOffset = oldOffset + size;
//					if (newOffset > data.Length)
//						break;

//					if (Interlocked.CompareExchange(ref offset, newOffset, oldOffset) == oldOffset)
//					{
//						buffer = new Memory<byte>(data, oldOffset, size);
//						Interlocked.Add(ref usage, size);

//						return true;
//					}
//				}

//				// cannot allocate the required amount
//				buffer = Memory<byte>.Empty;
//				return false;
//			}

//			public void Forget(in Memory<byte> buffer)
//			{
//				var success = MemoryMarshal.TryGetArray<byte>(buffer, out var segment);
//				Debug.Assert(success && segment.Array == data, "buffer is not owned by this pool");

//				var newValue = Interlocked.Add(ref usage, -buffer.Length);

//				Debug.Assert(newValue >= 0, "a buffer was released twice");
//			}
//		}

//		#endregion
//	}
//}

//#region [ License information          ]

///*

//Copyright (c) Attila Kiskó, enyim.com

//Licensed under the Apache License, Version 2.0 (the "License");
//you may not use this file except in compliance with the License.
//You may obtain a copy of the License at

//  http://www.apache.org/licenses/LICENSE-2.0

//Unless required by applicable law or agreed to in writing, software
//distributed under the License is distributed on an "AS IS" BASIS,
//WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//See the License for the specific language governing permissions and
//limitations under the License.

//*/

//#endregion
