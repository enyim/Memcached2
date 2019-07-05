using System;
using System.Collections.Generic;
using System.Linq;
using Enyim.Caching.Memcached;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Microsoft.Extensions.DependencyInjection
{
	internal class ClusterStartingStartupFilter : IStartupFilter
	{
		private IEnumerable<IMemcachedCluster> clusters;

		public ClusterStartingStartupFilter(IEnumerable<IMemcachedCluster> clusters)
		{
			this.clusters = clusters;
		}

		public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
		{
			return builder =>
			{
				foreach (var cluster in clusters)
				{
					if (!cluster.IsStarted)
						cluster.Start();
				}

				clusters = Enumerable.Empty<IMemcachedCluster>();

				next(builder);
			};
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
