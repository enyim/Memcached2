using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Enyim.Caching
{
	using System.Diagnostics;
	using IndexSetImpl = NodeQueue.ConcurrentIndexSet;

	/// <summary>
	/// This is a wrapper of BlockingCollection to prevent adding the same node to the queue multiple times.
	///
	/// Depending on the 'set' used inside, it may happen that a node gets added a couple of times,
	/// but it still won't monopolize the queue.
	/// </summary>
	internal class NodeQueue : IDisposable
	{
		public static readonly NodeQueue Empty = new NodeQueue(Array.Empty<INode>());

		private readonly BlockingCollection<INode> queue;
		private readonly IndexSetImpl set;
		private readonly Dictionary<INode, int> nodeIndexes;

		internal NodeQueue(INode[] allNodes)
		{
			queue = new BlockingCollection<INode>();
			set = new IndexSetImpl(allNodes.Length);
			nodeIndexes = Enumerable
									.Range(0, allNodes.Length)
									.ToDictionary(k => allNodes[k], k => k);
		}

		public void Dispose()
		{
			queue.Dispose();
		}

		public void Add(INode node)
		{
			if (set.Set(nodeIndexes[node]))
				queue.Add(node);
		}

		public INode Take(in CancellationToken token)
		{
			var retval = queue.Take(token);
			set.Unset(nodeIndexes[retval]);

			return retval;
		}

		#region [ ConcurrentIndexSet           ]

		internal class ConcurrentIndexSet
		{
			private const int SIZEOF_ITEM = 4;
			private const int CACHE_LINE_SIZE = 64;
			private const int STRIPE_LENGTH = CACHE_LINE_SIZE / SIZEOF_ITEM; // align each counter to a spearate cache-line

			private const int INDEX_OF_FLAG = 0;

			private const int TRUE = 1;
			private const int FALSE = 0;

			private readonly int[] data;

			public ConcurrentIndexSet(int capacity)
			{
				// add an extra padding to the beginning of the array to avoid false-sharing with the array's length
				data = new int[capacity * STRIPE_LENGTH + STRIPE_LENGTH];
			}

			public bool Set(int index)
			{
				var realIndex = STRIPE_LENGTH * (index + 1) + INDEX_OF_FLAG;

				return Interlocked.CompareExchange(ref data[realIndex], TRUE, FALSE) == FALSE;
			}

			public void Unset(int index)
			{
				var realIndex = STRIPE_LENGTH * (index + 1) + INDEX_OF_FLAG;

				Interlocked.Exchange(ref data[realIndex], FALSE);
			}
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
