using System;
using System.Runtime.CompilerServices;

namespace Enyim.Caching.Memcached
{
	/// <summary>
	/// Fails a node when the specified number of failures happen in a specified time window.
	/// </summary>
	public class ThrottlingFailurePolicyFactory : IFailurePolicyFactory
	{
		private TimeSpan resetAfter = TimeSpan.FromSeconds(10);
		private int threshold = 2;

		/// <summary>
		/// Specifies the time how long a node should function properly to reset its failure counter.
		/// </summary>
		public TimeSpan ResetAfter
		{
			get { return resetAfter; }
			set
			{
				Require.That(value > TimeSpan.Zero, "must be > TimeSpan.Zero");
				resetAfter = value;
			}
		}

		/// <summary>
		/// Specifies the number of failures that must occur in the specified time window to fail a node.
		/// </summary>
		public int Threshold
		{
			get { return threshold; }
			set
			{
				Require.That(value > 0, "must be > 0");
				threshold = value;
			}
		}

		public IFailurePolicy Create(INode node) => new Policy(node)
		{
			ResetAfter = ResetAfter,
			Threshold = Threshold
		};

		#region [ Policy                       ]

		private class Policy : IFailurePolicy
		{
#pragma warning disable CS0649 // field will be initialized by the LogTo rewriter
			private static readonly ILogger logger;
#pragma warning restore CS0649

			private readonly INode node;

			private int counter;
			private DateTime lastFailedUtc;

			public Policy(INode node)
			{
				this.node = node;
			}

			public TimeSpan ResetAfter { get; set; }
			public int Threshold { get; set; }

			public void Reset()
			{
				counter = 0;
			}

			public bool ShouldFail()
			{
				var now = DateTime.UtcNow;

				if (counter == 0)
				{
					logger.Trace("Node {node} never failed before, setting counter to 1", node);

					counter = 1;
				}
				else
				{
					var diff = now - lastFailedUtc;
					logger.Trace("Last fail for {node} was {diff} ago with counter {counter}", node, diff, counter);

					counter = diff <= ResetAfter ? (counter + 1) : 1;
				}

				lastFailedUtc = now;

				if (counter == Threshold)
				{
					logger.Information("Node {node} has reached threshold {threshold}, failing it", node, Threshold);
					counter = 0;

					return true;
				}

				logger.Trace("Node {node} has not reached threshold {threshold}; counter = {counter}", node, Threshold, counter);

				return false;
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
