using System;
using System.Diagnostics;

namespace Enyim.Caching.Memcached
{
	public class SharedClusterFixture<TServerFixture> : ClusterFixture<TServerFixture>
		where TServerFixture : IServerFixture, new()
	{
		private static readonly object clusterLock = new Object();
		private static IMemcachedCluster sharedCluster;
		private static TServerFixture sharedServers;

		private static int sharedRefCount;

		protected override IMemcachedCluster GetCluster()
		{
			lock (clusterLock)
			{
				if (sharedCluster == null)
				{
					sharedServers = Servers;
					sharedCluster = NewCluster(Servers);
				}

				sharedRefCount++;

				return sharedCluster;
			}
		}

		protected override void Dispose(bool disposing)
		{
			lock (clusterLock)
			{
				if (sharedRefCount > 0)
				{
					Debug.Assert(sharedCluster != null);

					sharedRefCount--;

					if (sharedRefCount == 0)
					{
						sharedCluster?.Dispose();
						sharedCluster = null;

						sharedServers.Dispose();
					}
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
