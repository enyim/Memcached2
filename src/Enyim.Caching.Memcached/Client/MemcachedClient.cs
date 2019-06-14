using System;
using System.Buffers;

namespace Enyim.Caching.Memcached
{
	public partial class MemcachedClient : IMemcachedClient
	{
		private readonly ITranscoder transcoder;
		private readonly ICluster cluster;
		private readonly IKeyTransformer keyTransformer;
		private readonly MemoryPool<byte> pool;

		public MemcachedClient(
			ICluster cluster,
			MemoryPool<byte>? pool = null,
			IKeyTransformer? keyTransformer = null,
			ITranscoder? transcoder = null)
		{
			this.cluster = cluster ?? throw new ArgumentNullException(nameof(cluster));
			if (!cluster.IsStarted) throw new InvalidOperationException("Cluster must be started before creating client instances");

			var thePool = pool ?? MemoryPool<byte>.Shared;
			this.pool = thePool;
			this.keyTransformer = keyTransformer ?? new Utf8KeyTransformer(thePool);

			this.transcoder = transcoder ?? new BinaryTranscoder();
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
