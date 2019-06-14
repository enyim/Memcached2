using System;
using System.Buffers;

namespace Enyim
{
	internal static class MemoryPoolExtensions
	{
		public static Span<T> Take<T>(this in Span<T> span, int length) => span.Slice(0, length);
		public static ReadOnlySpan<T> Take<T>(this in ReadOnlySpan<T> span, int length) => span.Slice(0, length);
		public static ReadOnlyMemory<T> Take<T>(this in ReadOnlyMemory<T> memory, int length) => memory.Slice(0, length);
		public static Memory<T> Take<T>(this in Memory<T> memory, int length) => memory.Slice(0, length);

		public static IMemoryOwner<T> RentExact<T>(this MemoryPool<T> pool, int size)
		{
			var retval = pool.Rent(size);
			if (retval.Memory.Length > size)
			{
				return new Wrapper<T>(retval, size);
			}

			return retval;
		}

		private class Wrapper<T> : IMemoryOwner<T>
		{
			private IMemoryOwner<T> original;

			public Wrapper(IMemoryOwner<T> original, int size)
			{
				this.original = original;
				Memory = original.Memory.Slice(0, size);
			}

			public Memory<T> Memory { get; private set; }

			public void Dispose()
			{
				var tmp = original;
				if (tmp != null)
				{
					tmp.Dispose();
					Memory = Memory<T>.Empty;
					original = OwnedMemory<T>.Empty;
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
