using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Enyim.Caching
{
	[DebuggerDisplay("[ Address: {endpoint}, IsAlive = {IsAlive} ]")]
	public class AsyncSocket : ISocket
	{
		#region [ Defaults                     ]

		public static class Defaults
		{
			public const int MaxBufferSize = 1 * 1024 * 1024;
			public const int MinBufferSize = 4096;

			public const int SendBufferSize = 64 * 1024;
			public const int ReceiveBufferSize = 64 * 1024;

			public static readonly TimeSpan ConnectionTimeoutMsec = TimeSpan.FromMilliseconds(10000);
			public static readonly TimeSpan SendTimeoutMsec = TimeSpan.FromMilliseconds(3000);
			public static readonly TimeSpan ReceiveTimeoutMsec = TimeSpan.FromMilliseconds(3000);
		}

		#endregion

#pragma warning disable CS0649 // field will be initialized by the LogTo rewriter
		private static readonly ILogger logger;
#pragma warning enable CS0649

		private readonly object ConnectLock = new object();

		private string name; // used for tracing
		private IPEndPoint? endpoint;

		private TimeSpan connectionTimeout;
		private TimeSpan requestTimeout;
		private TimeSpan responseTimeout;
		private int requestBufferSize;
		private int responseBufferSize;

		private Socket? socket;

		private int isAlive;
		private int isReceiving;
		private int isSending;

		private readonly MemoryPool<byte> allocator;

		private IMemoryOwner<byte> responseMemory;
		private SocketAsyncEventArgs? responseArgs;
		private ReadBuffer? responseBuffer;

		private IMemoryOwner<byte> requestMemory;
		private SocketAsyncEventArgs? requestArgs;
		private WriteBuffer? requestBuffer;

		internal protected AsyncSocket(MemoryPool<byte> allocator)
		{
			name = "";

			ConnectionTimeout = Defaults.ConnectionTimeoutMsec;
			RequestTimeout = Defaults.SendTimeoutMsec;
			ResponseTimeout = Defaults.ReceiveTimeoutMsec;

			RequestBufferSize = Defaults.SendBufferSize;
			ResponseBufferSize = Defaults.ReceiveBufferSize;

			responseMemory = OwnedMemory<byte>.Empty;
			requestMemory = OwnedMemory<byte>.Empty;

			this.allocator = allocator;
		}

		public IPEndPoint EndPoint => endpoint ?? throw new InvalidOperationException("Socket is not connected to any endpoints yet");

		public void Connect(IPEndPoint endpoint, CancellationToken token)
		{
			lock (ConnectLock)
			{
				PerformConnect(endpoint ?? throw new ArgumentNullException(nameof(endpoint)), token);
			}
		}

		private void PerformConnect(IPEndPoint endpoint, in CancellationToken token)
		{
			Debug.Assert(endpoint != null);

			this.endpoint = endpoint;
			name = endpoint.ToString();
			IsAlive = false;
			AsyncSocketEventSource.ConnectStart(name);

			var sw = Stopwatch.StartNew();

			using var mre = new ManualResetEventSlim(false);
			using var opt = new SocketAsyncEventArgs { RemoteEndPoint = endpoint };
			opt.Completed += (a, b) => mre.Set();

			RecreateSocket();
			Debug.Assert(socket != null);

			try
			{
				if (socket.ConnectAsync(opt)
					&& !mre.Wait((int)ConnectionTimeout.TotalMilliseconds, token))
				{
					AsyncSocketEventSource.ConnectFail(name, SocketError.TimedOut);
					Socket.CancelConnectAsync(opt);
					throw new IOException($"Connection timeout {ConnectionTimeout} has been exceeded while trying to connect to {endpoint}");
				}

				if (opt.SocketError != SocketError.Success)
				{
					AsyncSocketEventSource.ConnectFail(name, opt.SocketError);
					throw new IOException($"Could not connect to {endpoint}");
				}

				InitBuffers();
				IsAlive = true;
			}
			finally
			{
				logger.Information("Connected to {endpoint} in {elapsed} msec", endpoint, sw.ElapsedMilliseconds);
			}
		}

		#region [ Init                         ]

		private void InitBuffers()
		{
			if (requestBuffer == null)
			{
				// allocate the the buffers
				requestMemory = allocator.RentExact(RequestBufferSize);
				responseMemory = allocator.RentExact(ResponseBufferSize);

				requestBuffer = new WriteBuffer(requestMemory.Memory);
				responseBuffer = new ReadBuffer(responseMemory.Memory);

				// setup the outgoing channel
				requestArgs = new SocketAsyncEventArgs();
				requestArgs.Completed += RequestSent;
				requestArgs.SetBuffer(requestMemory.Memory);

				// setup the incoming channel
				responseArgs = new SocketAsyncEventArgs();
				responseArgs.Completed += ResponseReceived;
				responseArgs.SetBuffer(responseMemory.Memory);
			}
			else
			{
				Debug.Assert(responseBuffer != null);

				requestBuffer.Restart();
				responseBuffer.SetDataAvailable(0);
			}
		}

		private void RecreateSocket()
		{
			DestroySocket();

			socket = new Socket(SocketType.Stream, ProtocolType.Tcp)
			{
				NoDelay = true,
				ReceiveBufferSize = ResponseBufferSize,
				SendBufferSize = RequestBufferSize,
				ReceiveTimeout = ToTimeout(ResponseTimeout),
				SendTimeout = ToTimeout(RequestTimeout)
			};
		}

		#endregion

		public bool SendRequest(Action<bool> whenDone)
		{
			Debug.Assert(requestArgs != null);
			Debug.Assert(requestBuffer != null);

			AsyncSocketEventSource.SendStart(name, IsAlive, requestBuffer.Position);

			if (!IsAlive)
			{
				AsyncSocketEventSource.SendStop(name, IsAlive, false);
				whenDone(false);
				return false;
			}

			// already sending?
			if (Interlocked.CompareExchange(ref isSending, 1, 0) != 0)
			{
				return false;
			}

			requestArgs.UserToken = whenDone;
			PerformSend(requestBuffer.Slice());

			return true;
		}

		private void PerformSend(Memory<byte> data)
		{
			Debug.Assert(requestArgs != null);
			Debug.Assert(requestBuffer != null);
			Debug.Assert(socket != null);

			for (; ; )
			{
				// try sending all our data
				requestArgs.SetBuffer(data);
				// send is being done asynchrously (SendAsyncCompleted will clean up)
				if (socket.SendAsync(requestArgs))
				{
					break;
				}

				// send was done synchronously
				var sent = requestArgs.BytesTransferred;
				AsyncSocketEventSource.SendChunk(name, IsAlive, sent, requestArgs.SocketError);

				// check for fail
				if (requestArgs.SocketError != SocketError.Success || sent < 1)
				{
					// socket error
					FinishSending(false);
					break;
				}

				data = data.Slice(sent);

				// no data is remaining: quit
				// otherwise try sending a new chunk
				if (data.IsEmpty)
				{
					requestBuffer.Restart();
					FinishSending(true);
					break;
				}
			}
		}

		private void RequestSent(object sender, SocketAsyncEventArgs e)
		{
			Debug.Assert(requestArgs != null);
			Debug.Assert(requestBuffer != null);

			var sent = requestArgs.BytesTransferred;
			AsyncSocketEventSource.SendChunk(name, IsAlive, sent, requestArgs.SocketError);

			// failed during send
			if (requestArgs.SocketError != SocketError.Success || sent < 1)
			{
				FinishSending(false);
				return;
			}

			// OS sent less data than we asked for, so send the remaining data
			if (requestArgs.Count > sent)
			{
				PerformSend(requestArgs.MemoryBuffer.Slice(sent));
			}
			else
			{
				Debug.Assert(requestArgs.Count == sent);

				// all data was sent
				requestBuffer.Restart();
				FinishSending(true);
			}
		}

		private void FinishSending(bool success)
		{
			Debug.Assert(requestArgs != null);

			AsyncSocketEventSource.SendStop(name, IsAlive, success);

			Volatile.Write(ref isSending, 0);

			((Action<bool>)requestArgs.UserToken)?.Invoke(success);
		}

		public bool ReceiveResponse(Action<bool> whenDone)
		{
			Debug.Assert(responseArgs != null);
			Debug.Assert(socket != null);

			AsyncSocketEventSource.ReceiveStart(name, IsAlive);

			if (!IsAlive)
			{
				whenDone(false);
				return false;
			}

			if (Interlocked.CompareExchange(ref isReceiving, 1, 0) != 0)
			{
				return false;
			}

			responseArgs.UserToken = whenDone;

			if (!socket.ReceiveAsync(responseArgs))
			{
				// receive was done synchronously
				ResponseReceived(this, responseArgs);
			}

			return true;
		}

		private void ResponseReceived(object sender, SocketAsyncEventArgs recvArgs)
		{
			Debug.Assert(responseBuffer != null);

			var received = recvArgs.BytesTransferred;
			AsyncSocketEventSource.ReceiveChunk(name, IsAlive, received, recvArgs.SocketError);

			var success = recvArgs.SocketError == SocketError.Success && received > 0;
			responseBuffer.SetDataAvailable(success ? received : 0);

			AsyncSocketEventSource.ReceiveStop(name, IsAlive, success);
			Volatile.Write(ref isReceiving, 0);

			((Action<bool>)recvArgs.UserToken).Invoke(success);
		}

		private static int ToTimeout(TimeSpan time) => time == TimeSpan.MaxValue ? Timeout.Infinite : (int)time.TotalMilliseconds;

		#region [ Property noise               ]

		public bool IsAlive
		{
			get => Volatile.Read(ref isAlive) == 1;
			private set => Volatile.Write(ref isAlive, value ? 1 : 0);
		}

		public bool IsReceiving => Volatile.Read(ref isReceiving) == 1;
		public bool IsSending => Volatile.Read(ref isSending) == 1;
		public ReadBuffer ResponseBuffer => responseBuffer ?? throw new InvalidOperationException($"{nameof(ResponseBuffer)} cannot be accessed until the socket is connected");
		public WriteBuffer RequestBuffer => requestBuffer ?? throw new InvalidOperationException($"{nameof(RequestBuffer)} cannot be accessed until the socket is connected");

		private void ThrowIfConnected()
		{
			if (IsAlive)
				throw new InvalidOperationException("Cannot change socket options while connected.");
		}

		public TimeSpan ConnectionTimeout
		{
			get => connectionTimeout;
			set
			{
				ThrowIfConnected();
				Require.That(value >= TimeSpan.Zero, "must be >= TimeSpan.Zero");

				connectionTimeout = value;
			}
		}

		public TimeSpan RequestTimeout
		{
			get => requestTimeout;
			set
			{
				ThrowIfConnected();
				Require.That(value >= TimeSpan.Zero, "must be >= TimeSpan.Zero");

				requestTimeout = value;
			}
		}

		public TimeSpan ResponseTimeout
		{
			get => responseTimeout;
			set
			{
				ThrowIfConnected();
				Require.That(value >= TimeSpan.Zero, "must be >= TimeSpan.Zero");

				responseTimeout = value;
			}
		}

		public int RequestBufferSize
		{
			get => requestBufferSize;
			set
			{
				ThrowIfConnected();
				Require.That(value >= Defaults.MinBufferSize, "must be >= " + Defaults.MinBufferSize);
				Require.That(value <= Defaults.MaxBufferSize, "must be <= " + Defaults.MaxBufferSize);
				Require.That(value % 4096 == 0, "must be a multiply of 4k");

				requestBufferSize = value;
			}
		}

		public int ResponseBufferSize
		{
			get => responseBufferSize;
			set
			{
				ThrowIfConnected();
				Require.That(value >= Defaults.MinBufferSize, "must be >= " + Defaults.MinBufferSize);
				Require.That(value <= Defaults.MaxBufferSize, "must be <= " + Defaults.MaxBufferSize);
				Require.That(value % 4096 == 0, "must be a multiply of 4k");

				responseBufferSize = value;
			}
		}

		#endregion
		#region [ Cleanup                      ]

		public void Dispose()
		{
			GC.SuppressFinalize(this);

			lock (ConnectLock)
			{
				if (socket != null)
				{
					using (requestMemory)
					using (responseMemory)
					using (responseArgs)
					using (requestArgs)
					{
						if (requestArgs != null) requestArgs.Completed -= RequestSent;
						if (responseArgs != null) responseArgs.Completed -= ResponseReceived;
					}

					requestMemory = OwnedMemory<byte>.Empty;
					responseMemory = OwnedMemory<byte>.Empty;
					requestArgs = null;
					responseArgs = null;

					DestroySocket();
				}
			}
		}

		private void DestroySocket()
		{
			if (socket != null)
			{
				try
				{
					using (socket)
					{
						socket.Shutdown(SocketShutdown.Both);
					}
				}
				catch (Exception e)
				{
					logger.Information(e, "Exception while destroying socket {socket}.", name);
				}

				socket = null;
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
