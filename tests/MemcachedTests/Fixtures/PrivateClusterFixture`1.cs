using System;

namespace Enyim.Caching.Memcached
{
	public class PrivateClusterFixture<TServerFixture> : ClusterFixture<TServerFixture>
		where TServerFixture : IServerFixture, new()
	{
		private readonly object initLock;
		private IMemcachedCluster cluster;

		public PrivateClusterFixture()
		{
			initLock = new Object();
		}

		protected override IMemcachedCluster GetCluster()
		{
			lock (initLock)
			{
				if (cluster == null)
					cluster = NewCluster(Servers);

				return cluster;
			}
		}

		protected override void Dispose(bool disposing)
		{
			cluster?.Dispose();
			cluster = null;

			base.Dispose(disposing);
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
