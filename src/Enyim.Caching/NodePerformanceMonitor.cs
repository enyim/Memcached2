using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;

namespace Enyim.Caching
{
#if DIAGNOSTICS
	internal class NodePerformanceMonitor
	{
		private readonly ICounter opEnqueueWritePerSec;
		private readonly ICounter opDequeueWritePerSec;

		private readonly ICounter opEnqueueReadPerSec;
		private readonly ICounter opDequeueReadPerSec;

		private readonly ICounter writeQueueLength;
		private readonly ICounter readQueueLength;

		private readonly ICounter flushPerSec;
		private readonly ICounter errorsPerSec;
		private readonly ICounter opCount;

		public NodePerformanceMonitor(string endpoint)
		{
			opEnqueueWritePerSec = Metrics.Meter("node write request enqueue speed", endpoint, Interval.Seconds);
			opDequeueWritePerSec = Metrics.Meter("node write request dequeue speed", endpoint, Interval.Seconds);

			opEnqueueReadPerSec = Metrics.Meter("node read request enqueue speed", endpoint, Interval.Seconds);
			opDequeueReadPerSec = Metrics.Meter("node read request dequeue speed", endpoint, Interval.Seconds);

			writeQueueLength = Metrics.Counter("op write queue length", endpoint);
			readQueueLength = Metrics.Counter("op read queue length", endpoint);

			opCount = Metrics.Counter("commands", endpoint);
			errorsPerSec = Metrics.Meter("node errors/sec", endpoint, Interval.Seconds);
			flushPerSec = Metrics.Meter("node buffer send/sec", endpoint, Interval.Seconds);
		}


		private readonly IGauge opSendSpeed;
		private readonly IGauge opReceiveSpeed;

		public void ResetQueues()
		{
			writeQueueLength.Reset();
			readQueueLength.Reset();

			opEnqueueWritePerSec.Reset();
			opDequeueWritePerSec.Reset();
			opEnqueueReadPerSec.Reset();
			opDequeueReadPerSec.Reset();
		}

		public void EnqueueWriteOp()
		{
			opEnqueueWritePerSec.Increment();
			writeQueueLength.Increment();
		}

		public void DequeueWriteOp()
		{
			opDequeueWritePerSec.Increment();
			writeQueueLength.Decrement();
		}

		public void EnqueueReadOp()
		{
			opEnqueueReadPerSec.Increment();
			readQueueLength.Increment();
		}

		public void DequeueReadOp()
		{
			opDequeueReadPerSec.Increment();
			readQueueLength.Decrement();
		}

		public void NewOp()
		{
			opCount.Increment();
		}

		public void Error()
		{
			errorsPerSec.Increment();
		}

		public void Flush()
		{
			flushPerSec.Increment();
		}
	}
#else
	internal class NodePerformanceMonitor
	{
		public NodePerformanceMonitor(string endpoint)
		{
		}

		[Conditional("DIAGNOSTICS")]
		public void EnqueueWriteOp() { }

		[Conditional("DIAGNOSTICS")]
		public void DequeueWriteOp() { }

		[Conditional("DIAGNOSTICS")]
		public void EnqueueReadOp() { }

		[Conditional("DIAGNOSTICS")]
		public void DequeueReadOp() { }

		[Conditional("DIAGNOSTICS")]
		public void NewOp() { }

		[Conditional("DIAGNOSTICS")]
		public void Error() { }

		[Conditional("DIAGNOSTICS")]
		public void Flush() { }
	}
#endif
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
