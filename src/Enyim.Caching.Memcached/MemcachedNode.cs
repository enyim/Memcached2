using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Enyim.Caching.Memcached
{
	public class MemcachedNode : NodeBase
	{
#pragma warning disable CS0649 // field will be initialized by the LogTo rewriter
		private static readonly ILogger logger;
#pragma warning enable CS0649

		private const int SilentCountThreshold = 50;

		private readonly MemoryPool<byte> allocator;
		private int silentCount;
		private bool lastWasSilent;

		public MemcachedNode(MemoryPool<byte> allocator, ICluster owner, IPEndPoint endpoint, ISocket socket, IFailurePolicyFactory failurePolicyFactory)
			: base(owner, endpoint, socket, failurePolicyFactory)
		{
			this.allocator = allocator;
		}

		public override void Connect(CancellationToken token)
		{
			silentCount = 0;

			base.Connect(token);
		}

		protected override IResponse CreateResponse()
		{
			return new Operations.BinaryResponse(allocator);
		}

		public override Task<IOperation> Enqueue(IOperation op)
		{
			EnqueueNoOpIfNeeded(op);

			return base.Enqueue(op);
		}

		/// <summary>
		///  Add a NoOp after every {SilentCountThreshold}th continous silent op
		/// </summary>
		/// <param name="op"></param>
		private void EnqueueNoOpIfNeeded(IOperation op)
		{
			if (op is ICanBeSilent silent && silent.Silent)
			{
				silentCount++;
				logger.Trace("Node {node} got a silent op {op}, silentCount = {silentCount} ", this, op, silentCount);

				if (silentCount >= SilentCountThreshold)
				{
					silentCount = 0;
					logger.Trace("Node {node} has reached silent op count threshold {threshold}, injecting a NoOp", this, SilentCountThreshold);

					base.Enqueue(new Operations.NoOp(allocator));
				}
			}
		}

		protected override bool WriteOp(in OpQueueEntry data)
		{
			// remember the last op that was silent so that when we
			// run out of ops we'll know if we have to emit an additional NoOp
			// ***SSSSS<EOF>
			// * = normal
			// S = Silent
			// noop should be at <EOF> otherwise we won't get responses to the last
			// command until we get a new non-silent op queued up
			var silent = data.Op as ICanBeSilent;
			lastWasSilent = silent?.Silent == true;

			return base.WriteOp(data);
		}

		protected override OpQueueEntry GetNextOp()
		{
			var data = base.GetNextOp();

			// we've temporarily ran out of commands
			if (data.IsEmpty && lastWasSilent)
			{
				lastWasSilent = false;

				return new OpQueueEntry(new Operations.NoOp(allocator), new TaskCompletionSource<IOperation>(TaskCreationOptions.RunContinuationsAsynchronously));
			}

			return data;
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
