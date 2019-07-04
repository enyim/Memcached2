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

		public MemcachedClient(ICluster cluster, IMemcachedClientOptions? options = null)
		{
			if (options == null) options = new MemcachedClientOptions();

			this.cluster = cluster ?? throw new ArgumentNullException(nameof(cluster));
			if (!cluster.IsStarted) throw new ArgumentException("Cluster must be started before creating client instances", nameof(cluster));

			pool = options.Allocator ?? throw PropertyCannotBeNull(nameof(options.Allocator));
			keyTransformer = options.KeyTransformer ?? throw PropertyCannotBeNull(nameof(options.KeyTransformer));
			transcoder = options.Transcoder ?? throw PropertyCannotBeNull(nameof(options.Transcoder));
		}

		private static ArgumentNullException PropertyCannotBeNull(string property)
			=> new ArgumentNullException("options", $"Property options.{property} cannot be null");
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
