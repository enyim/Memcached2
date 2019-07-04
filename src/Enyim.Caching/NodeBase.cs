using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Enyim.Caching
{
	public abstract class NodeBase : INode
	{
#pragma warning disable CS0649 // field will be initialized by the LogTo rewriter
		private static readonly ILogger logger;
#pragma warning enable CS0649

		private readonly ICluster owner;
		private readonly IPEndPoint endpoint;
		private readonly ISocket socket;
		private readonly string name; // used for tracing
		private readonly Action<bool> flushWriteBufferAction;
		private readonly Action<bool> tryAskForMoreDataAction;
		private readonly IFailurePolicy failurePolicy;

		private readonly NodePerformanceMonitor npm;

		private bool isAlive; // if the socket is alive (ALIVE) or not (DEAD)
		private int currentlyReading; // if a read is in progress, to prevent re-entrancy on reads when a node is queued for IO multiple times
		private int currentlyWriting; // if a write is in progress, to prevent re-entrancy on writes when a node is queued for IO multiple times

		private readonly object failLock;

		private readonly ConcurrentQueue<OpQueueEntry> writeQueue;
		private readonly Queue<OpQueueEntry> readQueue;

		// this is the operatio currently being written into the request buffer
		// important:
		// - it's already dequeued from the write queue
		// - but it's not yet enqueued into the read queue
		private OpQueueEntry currentWriteOp;

		private IRequest? currentWriteCopier;
		private IResponse? inprogressResponse;

		private bool mustReconnect;

		protected NodeBase(ICluster owner, IPEndPoint endpoint, ISocket socket, IFailurePolicyFactory failurePolicyFactory)
		{
			this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
			this.endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
			this.socket = socket;

			name = endpoint.ToString();
			failLock = new object();
			writeQueue = new ConcurrentQueue<OpQueueEntry>();
			readQueue = new Queue<OpQueueEntry>();

			IsAlive = true;
			mustReconnect = true;

			flushWriteBufferAction = FlushWriteBufferRequest;
			tryAskForMoreDataAction = TryAskForMoreDataRequest;
			failurePolicy = (failurePolicyFactory ?? throw new ArgumentNullException(nameof(failurePolicyFactory))).Create(this);

			npm = new NodePerformanceMonitor(name);
		}

		protected abstract IResponse CreateResponse();
		public IPEndPoint EndPoint => endpoint;

		public bool IsAlive
		{
			get => Volatile.Read(ref isAlive);
			protected set => Volatile.Write(ref isAlive, value);
		}

		public virtual void Connect(CancellationToken token)
		{
			Debug.Assert(currentWriteCopier == null);
			Debug.Assert(inprogressResponse == null);
			Debug.Assert(readQueue.Count == 0);
			Debug.Assert(socket != null);

			logger.Information("Node is connecting to {endpoint}", name);

			mustReconnect = false;
			socket.Connect(endpoint, token);
		}

		public void Shutdown()
		{
			IsAlive = false;

			socket.Dispose();
		}

		public virtual Task<IOperation> Enqueue(IOperation op)
		{
			var tcs = new TaskCompletionSource<IOperation>(TaskCreationOptions.RunContinuationsAsynchronously);
			npm.NewOp();

			try
			{
				if (IsAlive)
				{
					writeQueue.Enqueue(new OpQueueEntry(op, tcs));

					#region Diagnostics
					npm.EnqueueWriteOp();
					NodeEventSource.EnqueueWriteOp(name);
					#endregion
				}
				else
				{
					tcs.TrySetException(new IOException(endpoint + " is not alive"));

					#region Diagnostics
					npm.Error();
					NodeEventSource.NodeError(name);
					#endregion
				}
			}
			catch (Exception e)
			{
				tcs.TrySetException(new IOException(endpoint + " enqueue failed. See inner exception for details.", e));

				#region Diagnostics
				npm.Error();
				NodeEventSource.NodeError(name);
				#endregion
			}

			return tcs.Task;
		}

		/// <summary>
		/// Processes (some of) the queued items; throws if an error occured
		/// </summary>
		public virtual void Run(CancellationToken cancellationToken)
		{
			try
			{
				NodeEventSource.RunStart(name);

				if (mustReconnect) Connect(cancellationToken);
				if (cancellationToken.IsCancellationRequested) return;

				if (!IsAlive)
				{
					var dead = new IOException($"Node is dead: {name}");
					FailQueue(writeQueue, dead);
					throw dead;
				}

				DoRun(cancellationToken);
			}
			catch (OperationCanceledException)
			{
				throw; // we are being shut down, no need for cleanup
			}
			catch (Exception e)
			{
				if (FailMe(e))
				{
					npm.Error();
					NodeEventSource.NodeError(name);

					throw;
				}
			}
			finally
			{
				NodeEventSource.RunStop(name);
			}
		}

		private void DoRun(CancellationToken cancellationToken)
		{
			if (TryStartWriting())
			{
				// write the current (in progress) op into the write buffer
				// - or -
				// start writing a new one (until we run out of ops or space)
				if (!ContinueWritingCurrentOp())
				{
					while (!socket.RequestBuffer.IsFull)
					{
						cancellationToken.ThrowIfCancellationRequested();

						var data = GetNextOp();
						if (data.IsEmpty) break;

						// it's possible that a command returns true ("needs more io")
						// but the request buffer is not full
						// (e.g. when writing the header)
						if (WriteOp(data)) goto full;
					}
				}

			full:
				cancellationToken.ThrowIfCancellationRequested();

				// did we write anything?
				if (socket.RequestBuffer.Position > 0)
				{
					FlushWriteBuffer();
				}
				else
				{
					// did not have any ops to send, quit
					FinishedWriting();
				}
			}

			if (TryStartReading())
			{
				cancellationToken.ThrowIfCancellationRequested();

				TryAskForMoreData();

				if (!socket.IsReceiving)
					TryProcessReceivedData();
			}
		}

		/// <summary>
		/// Sends the current chunked op. Happens when an op's data cannot fit the write buffer in one pass.
		/// </summary>
		/// <returns>returns true if further IO is required; false if no inprogress op present or the last chunk was successfully added to the buffer</returns>
		private bool ContinueWritingCurrentOp()
		{
			// check if we have an op in progress
			if (currentWriteCopier == null) return false;
			if (currentWriteCopier.WriteTo(socket.RequestBuffer)) return true;

			// last chunk was sent
			logger.Trace("Sent & finished {operation}", currentWriteOp.Op);

			// op is sent fully; response can be expected
			readQueue.Enqueue(currentWriteOp);

			#region Diagnostics
			npm.EnqueueReadOp();
			NodeEventSource.EnqueueReadOp(name);
			#endregion

			// clean up
			currentWriteCopier.Dispose();
			currentWriteCopier = null;
			currentWriteOp = default;

			return false;
		}

		protected virtual void FlushWriteBuffer()
		{
			npm.Flush();
			socket.SendRequest(flushWriteBufferAction);
		}

		private void FlushWriteBufferRequest(bool success)
		{
			if (success)
			{
				logger.Trace("Node {node} sent the write buffer successfully", name);

				FinishedWriting();
				owner.NeedsIO(this);
			}
			else
			{
				// this is a soft fail (cannot throw from other thread)
				// so we requeue for IO and Run() will throw instead
				logger.Trace("Node {node}'s FlushWriteBuffer failed", name);
				FailMe(new IOException("send fail"));
			}
		}

		protected virtual OpQueueEntry GetNextOp()
		{
			if (writeQueue.TryDequeue(out var data))
			{
				#region Diagnostics
				npm.DequeueWriteOp();
				NodeEventSource.DequeueWriteOp(name);
				#endregion

				return data;
			}

			return default;
		}

		/// <summary>
		/// <para>Writes an operation to the output buffer. Handles the case where the op does not fit the buffer fully.</para>
		/// <para>Returns true if further IO is needed to send the request.</para>
		/// </summary>
		/// <param name="data"></param>
		protected virtual bool WriteOp(in OpQueueEntry data)
		{
			if (currentWriteCopier != null)
				throw new InvalidOperationException("Cannot write an operation while another is in progress.");

			Debug.Assert(data.Op != null);

			currentWriteOp = data;
			IRequest request;

			try
			{
				request = data.Op.CreateRequest();
			}
			catch (Exception e)
			{
				logger.Error(e, "Cannot serialize operation {operation}", data.Op);
				data.Task.TrySetException(e);

				return false;
			}

			Debug.Assert(request != null, "WriteOp: request should have been initialized");

			if (!request.WriteTo(socket.RequestBuffer)) // no pending IO => fully written
			{
				readQueue.Enqueue(data);

				try { request.Dispose(); }
				catch (Exception e) { logger.Warning(e, "Error while disposing request of {operation}", data.Op); }

				currentWriteOp = default;
				Debug.Assert(currentWriteCopier == null);

				#region Diagnostics
				npm.EnqueueReadOp();
				NodeEventSource.EnqueueReadOp(name);

				logger.Trace("Full send of {operation}", data.Op);
				#endregion

				return false;
			}

			// it did not fit into the write buffer, so save the current op
			// as "in-progress"; DoRun will loop until it's fully sent
			currentWriteCopier = request;
			logger.Trace("Partial send of {operation}", data.Op);

			// current command request do not fit into the buffer
			return true;
		}

		private void TryAskForMoreData()
		{
			// no data to process => read the socket
			if (socket.ResponseBuffer.IsEmpty && !socket.IsReceiving)
			{
				logger.Trace("Read buffer is empty, asking for more.");

				socket.ReceiveResponse(tryAskForMoreDataAction);
			}
		}

		private void TryAskForMoreDataRequest(bool success)
		{
			if (success)
			{
				logger.Trace("Node {node} successfully received {count} bytes", name, socket.ResponseBuffer.Remaining);
				FinishedReading();
				owner.NeedsIO(this);
			}
			else
			{
				// this is a soft fail (cannot throw from other thread),
				// so we requeue for IO and exception will be thrown by Receive()
				FailMe(new IOException("Failed receiving from " + endpoint));
			}
		}

		private void TryProcessReceivedData()
		{
			// process the commands in the readQueue
			while (readQueue.Count > 0)
			{
				// continue filling the previously unfinished response,
				// or create a new one
				var response = inprogressResponse ?? CreateResponse();

				// continue filling the Response object from the buffer
				// Read() returns true if further data (IO) is required
				// (usually when the current response data is larger than the receive buffer size)
				if (response.Read(socket.ResponseBuffer))
				{
					inprogressResponse = response;
					logger.Trace("Response is not read fully, continue reading from the socket.");

					// refill the buffer
					FinishedReading();
					owner.NeedsIO(this);

					return;
				}

				try
				{
					// successfully read a response from the read buffer
					inprogressResponse = null;
					var isHandled = false;

					while (!isHandled && readQueue.Count > 0)
					{
						var data = readQueue.Peek();
						Debug.Assert(data.Op != null);

						// If the response does not matches the current op, it means it's a
						// response to a later command in the queue, so all commands before it
						// were silent commands without a response (== usually success).
						// So, successful silent ops will receive null as response (since
						// we have no real response (or we've ran into a bug))
						isHandled = data.Op.Handles(response);
						logger.Trace("Command {command} handles reponse {ishandled}", data.Op, isHandled);

						// operations are allowed to handle subsequent responses
						// they returns false when no more IO (response) is required => done processing
						if (!data.Op.ProcessResponse(isHandled ? response : null))
						{
							readQueue.Dequeue();
							data.Task.TrySetResult(data.Op);

							#region Diagnostics
							npm.DequeueReadOp();
							NodeEventSource.DequeueReadOp(name);
							#endregion
						}
					}
				}
				finally
				{
					response.Dispose();
				}
			}

			logger.Trace("Node {node} finished RECEIVE, unlock read", name);
			FinishedReading();
		}

		public override string ToString() => name;

		#region [ Busy signals                 ]

		private bool TryStartReading() => Interlocked.CompareExchange(ref currentlyReading, 1, 0) == 0;

		private bool TryStartWriting() => Interlocked.CompareExchange(ref currentlyWriting, 1, 0) == 0;

		private void FinishedReading() => Volatile.Write(ref currentlyReading, 0);

		private void FinishedWriting() => Volatile.Write(ref currentlyWriting, 0);

		#endregion
		#region [ OpQueueEntry                 ]

		protected readonly struct OpQueueEntry
		{
			public OpQueueEntry(IOperation op, TaskCompletionSource<IOperation> task)
			{
				Debug.Assert(op != null, "OpQueueEntry.Op cannot be null");
				Debug.Assert(task != null, "OpQueueEntry.Task cannot be null");

				Op = op;
				Task = task;
			}

			public bool IsEmpty => Op == default;

			public readonly IOperation Op;
			public readonly TaskCompletionSource<IOperation> Task;
		}

		#endregion
		#region [ Failure handlers             ]

		private bool FailMe(Exception e)
		{
			lock (failLock)
			{
				logger.Error(e, "Node {node} has failed during IO.", name);
				var fail = (e is IOException) ? e : new IOException("io fail; see inner exception", e);
				var didFail = false;

				// mark as dead if policy says so...
				if (failurePolicy.ShouldFail())
				{
					logger.Information("Current failure policy marked the node {node} as dead", name);

					IsAlive = false;
					didFail = true;
				}

				FinishedReading();
				FinishedWriting();

				// kill the partially sent op (if any)
				if (!currentWriteOp.IsEmpty)
				{
					currentWriteOp.Task.TrySetException(fail);
					currentWriteOp = default;
				}

				// kill the partially written command
				try { currentWriteCopier?.Dispose(); } catch { }
				currentWriteCopier = default;

				// kill the partially read response
				try { inprogressResponse?.Dispose(); } catch { }
				inprogressResponse = default;

				FailQueue(readQueue, fail);

				if (didFail)
				{
					// this is a hard-fail, all queued operations should be failed
					// (otherwise we'll just retry them after reconnecting)
					// we'll clean thuis as the last, to let the IsAlive=false to propagate
					// (which will in turn fail all subsequent Enqueues, so FailQueue has a chance to actually empty the queue)
					FailQueue(writeQueue, fail);
				}
				else
				{
					// ...otherwise reconnect immediately (when it's our turn)
					mustReconnect = true;
					logger.Information("Current failure policy marked this as a soft-fail, queueing node {node} for reconnect.", name);

					// reconnect from IO thread
					owner.NeedsIO(this);
				}

				return didFail;
			}
		}

		/// <summary>
		/// Cleans up a Queue, marking all items as failed
		/// </summary>
		private void FailQueue(Queue<OpQueueEntry> queue, Exception e)
		{
			foreach (var data in queue)
			{
				if (data.Op == null) continue;

				data.Op.Failed(e);
				data.Task.TrySetException(e);

				npm.Error();
				NodeEventSource.NodeError(name);
			}

			queue.Clear();
		}

		/// <summary>
		/// Cleans up a ConcurrentQueue, marking all items as failed
		/// </summary>
		private void FailQueue(ConcurrentQueue<OpQueueEntry> queue, Exception e)
		{
			while (queue.TryDequeue(out var data))
			{
				if (data.Op == null) continue;

				data.Op.Failed(e);
				data.Task.TrySetException(e);
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
