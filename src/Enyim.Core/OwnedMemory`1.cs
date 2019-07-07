using System;
using System.Buffers;

namespace Enyim
{
	public static class OwnedMemory<T>
	{
		public static readonly IMemoryOwner<T> Empty = new EmptyOwned();

		private class EmptyOwned : IMemoryOwner<T>
		{
			public Memory<T> Memory { get; } = Memory<T>.Empty;
			public void Dispose() { }
		}
	}

	public static class OwnedMemoryExtensions
	{
		public static bool IsEmpty<T>(this IMemoryOwner<T>? self) => self?.Memory.IsEmpty != false;
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
