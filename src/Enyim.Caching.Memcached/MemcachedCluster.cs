using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Enyim.Caching.Memcached
{
	public class MemcachedCluster : ClusterBase
	{
		private readonly MemoryPool<byte> allocator;
		private readonly IFailurePolicyFactory failurePolicyFactory;
		private readonly ISocketFactory socketFactory;

		/// <param name="endpoints">List of host names or ip addresses with optional port specifiers, separated by ',' or ';'. E.g. "10.0.0.1,10.0.0.2:11200"</param>
		public MemcachedCluster(string endpoints, IMemcachedClusterOptions? options = null)
			: this(ParseEndPoints(endpoints ?? throw new ArgumentNullException(nameof(endpoints))), options) { }

		public MemcachedCluster(IEnumerable<string> endpoints, IMemcachedClusterOptions? options = null)
			: this(ParseEndPoints(endpoints ?? throw new ArgumentNullException(nameof(endpoints))), options) { }

		public MemcachedCluster(IEnumerable<IPEndPoint> endpoints, IMemcachedClusterOptions? options = null)
			: this(endpoints, options ?? new MemcachedClusterOptions(), false) { }

		private MemcachedCluster(IEnumerable<IPEndPoint> endpoints, IMemcachedClusterOptions options, bool _)
			: base(endpoints,
					options.Locator ?? throw PropertyCannotBeNull(nameof(options.Locator)),
					options.ReconnectPolicyFactory ?? throw PropertyCannotBeNull(nameof(options.ReconnectPolicyFactory)))
		{
			allocator = options.Allocator ?? throw PropertyCannotBeNull(nameof(options.Allocator));
			socketFactory = options.SocketFactory ?? throw PropertyCannotBeNull(nameof(options.SocketFactory));
			failurePolicyFactory = options.FailurePolicyFactory ?? throw PropertyCannotBeNull(nameof(options.FailurePolicyFactory));
		}

		protected override INode CreateNode(IPEndPoint endpoint)
			=> new MemcachedNode(allocator, this, endpoint, socketFactory.Create(), failurePolicyFactory);

		private static IEnumerable<IPEndPoint> ParseEndPoints(string value)
			=> ParseEndPoints(value.Split(new char[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries));

		private static IEnumerable<IPEndPoint> ParseEndPoints(IEnumerable<string> value)
			=> value
					.Where(v => !String.IsNullOrEmpty(v))
					.Select(v => EndPointHelper.Parse(v.Trim()))
					.ToArray();

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
