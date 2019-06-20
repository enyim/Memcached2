using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;

namespace Enyim.Caching
{
	public abstract class ClusterBase : ICluster
	{
#pragma warning disable CS0649 // field will be initialized by the LogTo rewriter
		private static readonly ILogger logger;
#pragma warning enable CS0649

		private readonly IPEndPoint[] endpoints;
		private readonly INodeLocator locator;
		private readonly Dictionary<INode, IReconnectPolicy> reconnectPolicies;
		private readonly IReconnectPolicyFactory reconnectPolicyFactory;

		private readonly object StateLock;
		private readonly CancellationTokenSource shutdownToken;

		private readonly Thread worker;
		private readonly ManualResetEventSlim workerIsDone;

		private INode[]? allNodes; // all nodes in the cluster known by us
		private INode[]? workingNodes; // the nodes that are still working
		private NodeQueue ioQueue; // the nodes that has IO pending

		protected ClusterBase(IEnumerable<IPEndPoint> endpoints, INodeLocator locator, IReconnectPolicyFactory reconnectPolicyFactory)
		{
			var finalEndpoints = (endpoints ?? throw new ArgumentNullException(nameof(endpoints))).ToArray();
			if (finalEndpoints.Length < 1) throw new ArgumentException("Must provide at least one endpoint to connect to", nameof(endpoints));

			this.endpoints = finalEndpoints;
			this.locator = locator ?? throw new ArgumentNullException(nameof(locator));
			this.reconnectPolicies = new Dictionary<INode, IReconnectPolicy>();
			this.reconnectPolicyFactory = reconnectPolicyFactory;
			this.ioQueue = NodeQueue.Empty;

			StateLock = new object();
			shutdownToken = new CancellationTokenSource();
			workerIsDone = new ManualResetEventSlim(false);
			worker = new Thread(Worker) { Name = "IO Thread {" + String.Join(", ", finalEndpoints.Select(e => e.ToString())) + "}" };
		}

		protected bool IsDisposed { get; private set; }
		protected abstract INode CreateNode(IPEndPoint endpoint);

		public bool IsStarted => allNodes != null;

		public void Start()
		{
			lock (StateLock)
			{
				if (IsStarted) throw new InvalidOperationException("Cluster is already started");
				if (IsDisposed) throw new ObjectDisposedException("Cluster is already disposed");

				OnStarting();

				allNodes = endpoints.Select(CreateNode).ToArray();
				ioQueue = new NodeQueue(allNodes);
				workingNodes = allNodes.ToArray();
				allNodes.IntoDictionary(reconnectPolicies, n => n, reconnectPolicyFactory.Create);

				locator.Initialize(allNodes);
				worker.Start();

				OnStarted();
			}
		}

		protected virtual void OnStarting() { }
		protected virtual void OnStarted() { }

		Task ICluster.Execute(IItemOperation op)
		{
			var node = locator.Locate(op.Key.Span);

			if (node == null) throw new IOException("All nodes are dead");
			if (!node.IsAlive) throw new IOException($"Node {node} is dead");

			var retval = node.Enqueue(op);
			ioQueue.Add(node);

			return retval;
		}

		Task<IOperation>[] ICluster.Broadcast<TState>(Func<INode, TState, IOperation> createOp, TState state)
		{
			Debug.Assert(workingNodes != null);

			// create local "copy" of the reference, as
			// workingNodes is never changed but replaced
			var nodes = workingNodes;
			var tasks = new Task<IOperation>[nodes.Length];
			var i = 0;

			foreach (var node in nodes)
			{
				//if (node.IsAlive)
				{
					tasks[i++] = node.Enqueue(createOp(node, state));
					ioQueue.Add(node);
				}
			}

			if (i == 0) throw new IOException("All nodes are dead");
			//if (i != tasks.Length) Array.Resize(ref tasks, i);

			return tasks;
		}

		/// <summary>
		/// Put the node into the pending work queue.
		/// </summary>
		/// <param name="node"></param>
		void ICluster.NeedsIO(INode node)
		{
			logger.Trace("Node {node} is requeued for IO", node);
			ioQueue.Add(node);
		}

		private void Worker()
		{
			while (!shutdownToken.IsCancellationRequested)
			{
				try
				{
					var node = ioQueue.Take(shutdownToken.Token);

					try
					{
						node.Run(shutdownToken.Token); // this may throw

						logger.Trace("Node {node} finished IO", node);
						if (shutdownToken.IsCancellationRequested) break;
					}
					catch (OperationCanceledException)
					{
						break;
					}
					catch (Exception e)
					{
						HandleFailedNode(node, e);
					}
				}
				catch (OperationCanceledException)
				{
					break;
				}
			}

			logger.Trace("shutdownToken was cancelled, finishing work");

			workerIsDone.Set();
		}

		/// <summary>
		/// Marks a node as failed.
		/// - it removes from the known list of nodes and reinitializes the node locator
		/// - schedules the node for reconnect (based on the current IReconnectPolicy)
		/// </summary>
		/// <param name="node">The failed node</param>
		/// <param name="e">The reason of the failure</param>
		/// <remarks>Only called from the IO thread.</remarks>
		protected void HandleFailedNode(INode node, Exception e)
		{
			logger.Error(e, "Node {node} has failed", node);

			// serialize the reconnect attempts to make
			// IReconnectPolicy and INodeLocator implementations simpler
			lock (StateLock)
			{
				if (IsDisposed) return;

				var original = Volatile.Read(ref workingNodes);

				// even though we're locking we still do the CAS,
				// because the IO thread does not use any locking
				while (true)
				{
					var updated = original.Where(n => n != node).ToArray();
					var previous = Interlocked.CompareExchange(ref workingNodes, updated, original);

					if (ReferenceEquals(original, previous))
					{
						locator.Initialize(updated);
						break;
					}

					original = previous;
				}
			}

			ScheduleReconnect(node);
		}

		/// <summary>
		/// Schedules a failed node for reconnection.
		/// </summary>
		/// <param name="node"></param>
		protected virtual void ScheduleReconnect(INode node)
		{
			logger.Information("Scheduling reconnect for node {node}", node);

			var tmp = reconnectPolicies.TryGetValue(node, out var reconnectPolicy);
			Debug.Assert(tmp, "no reconnectPolicy is defined for node " + node);

			var when = reconnectPolicy.Schedule();

			if (when == TimeSpan.Zero)
			{
				logger.Information("When = 0, node {node} will reconnect immediately", node);

				Task.Run(() => ReconnectNow(node), shutdownToken.Token);
			}
			else
			{
				logger.Information("Queueing reconnect for node {node} with delay {when}", node, when);
				Task
					.Delay(when, shutdownToken.Token)
					.ContinueWith((_, n) => ReconnectNow((INode)n), node, TaskContinuationOptions.OnlyOnRanToCompletion
																			| TaskContinuationOptions.LongRunning
																			| TaskContinuationOptions.RunContinuationsAsynchronously);
			}
		}

		protected void ReconnectNow(INode node)
		{
			try
			{
				if (shutdownToken.IsCancellationRequested) return;

				node.Connect(shutdownToken.Token);

				ReAddNode(node);
				ioQueue.Add(node); // trigger IO on this node
			}
			catch (OperationCanceledException)
			{
				logger.Information("Cluster was shut down during reconnect, aborting.");
			}
			catch (Exception e)
			{
				if (shutdownToken.IsCancellationRequested)
				{
					logger.Error(e, "Error occured, but cluster was shut down during reconnect, so ignoring it");
					return;
				}

				logger.Error(e, "Failed to reconnect {node}, rescheduling", node);

				// TODO this can lead to inifinite loops
				// for now it's the reconnect policy's responsibility to prevent it
				ScheduleReconnect(node);
			}
		}

		/// <summary>
		/// Mark the specified node as working.
		/// </summary>
		/// <param name="node"></param>
		/// <remarks>Can be called from a background thread (Task pool)</remarks>
		protected void ReAddNode(INode node)
		{
			logger.Information("Node {node} was reconnected", node);

			// serialize the reconnect attempts to make
			// IReconnectPolicy and INodeLocator implementations simpler
			lock (StateLock)
			{
				var tmp = reconnectPolicies.TryGetValue(node, out var reconnectPolicy);
				Debug.Assert(tmp, "no reconnectPolicy is defined for node " + node);

				reconnectPolicy.Reset();

				var original = Volatile.Read(ref workingNodes);

				// even though we're locking we still do the CAS,
				// because the IO thread does not use any locking
				while (true)
				{
					// prepend new node to the list of working nodes
					// to make it reconnect as fast as possible
					Debug.Assert(original != null);

					var updated = new INode[original.Length + 1];
					updated[0] = node;
					Array.Copy(original, 0, updated, 1, original.Length);

					var previous = Interlocked.CompareExchange(ref workingNodes, updated, original);
					if (ReferenceEquals(original, previous))
					{
						locator.Initialize(updated);
						break;
					}

					original = previous;
				}
			}
		}

		public void Dispose()
		{
			if (IsDisposed) return;

			shutdownToken.Cancel();
			workerIsDone.Wait();

			// if the cluster is not stopped yet, we should clean up
			lock (StateLock)
			{
				// ??
				if (IsDisposed) return;

				IsDisposed = true;
				Debug.Assert(allNodes != null);
				Debug.Assert(workingNodes != null);

				try
				{
					Disposing(true);

					using (shutdownToken)
					using (ioQueue)
					using (workerIsDone)
					{
						foreach (var node in allNodes)
						{
							try { node.Shutdown(); }
							catch (Exception e)
							{
								logger.Error(e, "Error while shutting down node {node}", node);
							}
						}

						Array.Clear(allNodes, 0, allNodes.Length);
						Array.Clear(workingNodes, 0, workingNodes.Length);
					}
				}
				catch (Exception e)
				{
					logger.Warning(e, "Error while disposing cluster {cluster}", "{" + String.Join(", ", endpoints.Select(e => e.ToString())) + "}");
				}
			}
		}

		protected virtual void Disposing(bool disposing) { }
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
