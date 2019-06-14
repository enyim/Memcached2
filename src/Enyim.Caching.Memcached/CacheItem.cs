using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Enyim.Caching.Memcached
{
	public readonly struct CacheItem : IDisposable
	{
		//public static readonly CacheItem Empty = new CacheItem(0, null);

		private readonly uint flags;
		private readonly IMemoryOwner<byte> data;

		public CacheItem(uint flags, IMemoryOwner<byte> data)
		{
			this.flags = flags;
			this.data = data;
		}

		public uint Flags => flags;
		public IMemoryOwner<byte> Data => data;

		public void Dispose() => data?.Dispose();

		public override int GetHashCode() => HashCode.Combine(flags.GetHashCode(), data.GetHashCode());
		public override bool Equals(object obj) => Equals((CacheItem)obj);
		public bool Equals(in CacheItem obj) => obj.flags == flags && obj.data.Equals(data);
		public static bool operator ==(in CacheItem a, in CacheItem b) => a.Equals(b);
		public static bool operator !=(in CacheItem a, in CacheItem b) => !a.Equals(b);
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
