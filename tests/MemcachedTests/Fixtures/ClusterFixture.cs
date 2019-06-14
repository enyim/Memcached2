using System;
using System.Buffers;
using System.Diagnostics;

namespace Enyim.Caching.Memcached
{
	public abstract class ClusterFixture<TServerFixture> : IClusterFixture
		where TServerFixture : IServerFixture, new()
	{
		private readonly object initLock;
		private MemcachedClient client;

		protected ClusterFixture()
		{
			initLock = new Object();
			Servers = new TServerFixture();
		}

		protected TServerFixture Servers { get; }

		protected abstract ICluster GetCluster();
		public virtual MemcachedClient CreateClient() => client ?? (client = NewClient(GetCluster()));

		void IDisposable.Dispose()
		{
			lock (initLock)
			{
				Dispose(true);
			}
		}

		protected virtual void Dispose(bool disposing)
			=> Servers?.Dispose();

		protected static MemcachedClient NewClient(ICluster cluster)
			=> new MemcachedClient(cluster, MemoryPool<byte>.Shared, new Utf8KeyTransformer(MemoryPool<byte>.Shared), new BinaryTranscoder());

		protected static ICluster NewCluster(TServerFixture servers)
		{
			var retval = new MemcachedCluster(servers.Run(),
							socketFactory:
								Debugger.IsAttached ? new AsyncSocketFactory(new SocketOptions { ConnectionTimeout = TimeSpan.FromSeconds(3600) })
								: null
						);

			retval.Start();

			return retval;
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
