using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Enyim.Caching.Memcached
{
	public class MemcachedCluster : ClusterBase
	{
		private readonly MemoryPool<byte> pool;
		private readonly IFailurePolicyFactory failurePolicyFactory;
		private readonly ISocketFactory socketFactory;

		/// <param name="endpoints">List of host names or ip addresses with optional port specifiers, separated by ',' or ';'. E.g. "10.0.0.1,10.0.0.2:11200"</param>
		public MemcachedCluster(string endpoints, INodeLocator? locator = null, IReconnectPolicyFactory? reconnectPolicy = null, IFailurePolicyFactory? failurePolicyFactory = null, ISocketFactory? socketFactory = null, MemoryPool<byte>? pool = null)
			: this(ParseEndPoints(endpoints), locator, reconnectPolicy, failurePolicyFactory, socketFactory, pool) { }

		public MemcachedCluster(IEnumerable<string> endpoints, INodeLocator? locator = null, IReconnectPolicyFactory? reconnectPolicy = null, IFailurePolicyFactory? failurePolicyFactory = null, ISocketFactory? socketFactory = null, MemoryPool<byte>? pool = null)
			: this(ParseEndPoints(endpoints), locator, reconnectPolicy, failurePolicyFactory, socketFactory, pool) { }

		public MemcachedCluster(
			IEnumerable<IPEndPoint> endpoints,
			INodeLocator? locator = null,
			IReconnectPolicyFactory? reconnectPolicy = null,
			IFailurePolicyFactory? failurePolicyFactory = null,
			ISocketFactory? socketFactory = null,
			MemoryPool<byte>? pool = null)
			: base(endpoints,
					locator ?? new DefaultNodeLocator(),
					reconnectPolicy ?? new PeriodicReconnectPolicyFactory())
		{
			this.pool = pool ?? MemoryPool<byte>.Shared;
			this.socketFactory = socketFactory ?? new AsyncSocketFactory();
			this.failurePolicyFactory = failurePolicyFactory ?? new ImmediateFailurePolicyFactory();
		}

		protected override INode CreateNode(IPEndPoint endpoint)
			=> new MemcachedNode(pool, this, endpoint, socketFactory.Create(), failurePolicyFactory);

		private static IEnumerable<IPEndPoint> ParseEndPoints(string value)
			=> ParseEndPoints((value ?? throw new ArgumentNullException(nameof(value)))
						.Split(new char[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries));

		private static IEnumerable<IPEndPoint> ParseEndPoints(IEnumerable<string> value)
			=> (value ?? throw new ArgumentNullException(nameof(value)))
					.Where(v => !String.IsNullOrEmpty(v))
					.Select(v => EndPointHelper
									.ParseEndPoint(v.Trim(), Protocol.DefaultPort));
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
