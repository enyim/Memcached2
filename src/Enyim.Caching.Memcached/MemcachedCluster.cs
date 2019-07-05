using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using Enyim.Caching.Memcached.Internal;

namespace Enyim.Caching.Memcached
{
	public class MemcachedCluster : ClusterBase, IMemcachedCluster
	{
		private readonly object clientLock = new Object();

		private readonly MemoryPool<byte> allocator;
		private readonly IFailurePolicyFactory failurePolicyFactory;
		private readonly IMemcachedClientOptions clientOptions;
		private readonly ISocketFactory socketFactory;
		private IMemcachedClient? defaultClient;

		/// <param name="endpoints">List of host names or ip addresses with optional port specifiers, separated by ',' or ';'. E.g. "10.0.0.1,10.0.0.2:11200"</param>
		public MemcachedCluster(string endpoints,
									IMemcachedClusterOptions? clusterOptions = null,
									IMemcachedClientOptions? clientOptions = null)
			: this(EndPointHelper.ParseList(endpoints ?? throw new ArgumentNullException(nameof(endpoints))),
					clusterOptions,
					clientOptions)
		{ }

		public MemcachedCluster(IEnumerable<string> endpoints,
									IMemcachedClusterOptions? clusterOptions = null,
									IMemcachedClientOptions? clientOptions = null)
			: this(EndPointHelper.ParseList(endpoints ?? throw new ArgumentNullException(nameof(endpoints))),
					clusterOptions,
					clientOptions)
		{ }

		public MemcachedCluster(IEnumerable<IPEndPoint> endpoints,
									IMemcachedClusterOptions? clusterOptions = null,
									IMemcachedClientOptions? clientOptions = null)
			: this(endpoints ?? throw new ArgumentNullException(nameof(endpoints)),
					clusterOptions ?? new MemcachedClusterOptions(),
					clientOptions ?? new MemcachedClientOptions(), false)
		{ }

		private MemcachedCluster(IEnumerable<IPEndPoint> endpoints,
									IMemcachedClusterOptions clusterOptions,
									IMemcachedClientOptions clientOptions,
									bool _)
			: base(endpoints,
					clusterOptions.Locator ?? throw PropertyCannotBeNull(nameof(clusterOptions.Locator)),
					clusterOptions.ReconnectPolicyFactory ?? throw PropertyCannotBeNull(nameof(clusterOptions.ReconnectPolicyFactory)))
		{
			allocator = clusterOptions.Allocator ?? throw PropertyCannotBeNull(nameof(clusterOptions.Allocator));
			socketFactory = clusterOptions.SocketFactory ?? throw PropertyCannotBeNull(nameof(clusterOptions.SocketFactory));
			failurePolicyFactory = clusterOptions.FailurePolicyFactory ?? throw PropertyCannotBeNull(nameof(clusterOptions.FailurePolicyFactory));

			this.clientOptions = clientOptions;
		}

		protected override INode CreateNode(IPEndPoint endpoint)
			=> new MemcachedNode(allocator, this, endpoint, socketFactory.Create(), failurePolicyFactory);

		private static ArgumentNullException PropertyCannotBeNull(string property)
			=> new ArgumentNullException("options", $"Property options.{property} cannot be null");

		public IMemcachedClient GetClient(IMemcachedClientOptions? customOptions = null)
		{
			if (customOptions == null)
			{
				var retval = Volatile.Read(ref defaultClient);
				if (retval == null)
				{
					retval = new MemcachedClient(this, clientOptions);

					// is somebody else was faster return the already created instance
					return Interlocked.CompareExchange(ref defaultClient, retval, null) ?? retval;
				}

				return retval;
			}

			return new MemcachedClient(this, customOptions);
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
