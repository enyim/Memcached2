using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Enyim.Caching.Memcached
{
	public class DefaultNodeLocator : INodeLocator
	{
		private InnerLocator locator = new InnerLocator(Enumerable.Empty<INode>());

		public void Initialize(IEnumerable<INode> nodes) => Interlocked.Exchange(ref locator, new InnerLocator(nodes));
		public INode Locate(ReadOnlySpan<byte> key) => locator.Locate(key);

		#region [ InnerLocator                 ]

		private class InnerLocator
		{
			private readonly INode[] nodes;
			private readonly int bucketCount;

			public InnerLocator(IEnumerable<INode> currentNodes)
			{
				nodes = currentNodes.ToArray();
				bucketCount = nodes.Length;
			}

			public INode Locate(in ReadOnlySpan<byte> key)
			{
				if (bucketCount == 0) return AlreadyFailedNode.Instance;
				if (bucketCount == 1) return nodes[0];

				var (_, high) = MurmurHash3.ComputeHash128(key);
				var bucketIndex = JumpConsistentHash(high, bucketCount);

				return nodes[bucketIndex];
			}

			private static int JumpConsistentHash(ulong key, int bucketCount)
			{
				Debug.Assert(bucketCount > 0);

				const ulong MULTIPLIER = 2_862_933_555_777_941_757;

				var retval = 0UL;
				var index = 0UL;
				var ulongCount = (ulong)bucketCount;

				while (index < ulongCount)
				{
					retval = index;
					key = (key * MULTIPLIER) + 1;
					index = (ulong)((retval + 1) * (double)(1L << 31) / ((key >> 33) + 1));
				}

				return (int)retval;
			}
		}

		#endregion
		#region [ AlreadyFailedNode            ]

		private class AlreadyFailedNode : INode
		{
			private static readonly Task<IOperation> FailedTask = Task.FromException<IOperation>(new IOException("NoNodesAreAvailable"));

			public static readonly INode Instance = new AlreadyFailedNode();

			public bool IsAlive { get; } = false;
			public IPEndPoint EndPoint { get; } = new IPEndPoint(IPAddress.None, 0);

			public void Connect(CancellationToken token) { }
			public void Run(CancellationToken token) { }
			public Task<IOperation> Enqueue(IOperation op) => FailedTask;
			public void Shutdown() { }

			public override string ToString() => "NoNodesAreAvailable";
		}

		#endregion
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
