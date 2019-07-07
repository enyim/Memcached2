using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Enyim.Caching.Memcached
{
	public class MemcachedClientOptions : IMemcachedClientOptions
	{
		//private MemoryPool<byte> allocator;
		private IKeyFormatter keyFormatter;
		private IItemFormatter itemFormatter;

		public MemcachedClientOptions()
		{
			//allocator = MemoryPool<byte>.Shared;
			keyFormatter = new Utf8KeyFormatter();
			itemFormatter = new BinaryItemFormatter();
		}

		//public MemoryPool<byte> Allocator
		//{
		//	get => allocator;
		//	set => allocator = value ?? throw PropertyCannotBeNull();
		//}

		public IKeyFormatter KeyFormatter
		{
			get => keyFormatter;
			set => keyFormatter = value ?? throw PropertyCannotBeNull();
		}

		public IItemFormatter ItemFormatter
		{
			get => itemFormatter;
			set => itemFormatter = value ?? throw PropertyCannotBeNull();
		}

		private static ArgumentNullException PropertyCannotBeNull([CallerMemberName] string? property = null)
			=> new ArgumentNullException("value", $"Property {property} cannot be null");
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
