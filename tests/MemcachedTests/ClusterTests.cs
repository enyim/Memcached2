using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Enyim.Caching.Memcached
{
	public class ClusterTests
	{
		[Fact(Timeout = 10000)]
		public async Task Dead_Node_Should_Not_Block_Dispose()
		{
			using var c = new MockCluster(EP("localhost:12000", "localhost:12002", "localhost:12004", "localhost:12006"),
											new DefaultNodeLocator(),
											new ImmediateReconnectPolicyFactory(),
											new FailNodeFactory());
			{
				c.Start();
				try { await Task.WhenAll(c.Broadcast(_ => new WorkingOperation())); } catch { }

				await Task.Delay(1000);
			}
		}

		[Fact(Timeout = 1000)]
		public async Task Exception_Should_Properly_Bubble_Up()
		{
			using var c = new MockCluster(EP("localhost:12000"),
											new DefaultNodeLocator(),
											new PeriodicReconnectPolicyFactory(),
											new WorkingNodeFactory());

			c.Start();

			var e = await Assert.ThrowsAsync<IOException>(() => c.Execute(new FailingOperation()));
			var nie = Assert.IsType<NotImplementedException>(e.InnerException);
			Assert.Equal(FailingOperation.ExceptionMessage, nie.Message);
		}

		private static IPEndPoint[] EP(params string[] list) => list.Select(EndPointHelper.Parse).ToArray();

		#region [ Mocks                        ]

		private class MockCluster : ClusterBase
		{
			private readonly NodeFactory nodeFactory;

			public MockCluster(IEnumerable<IPEndPoint> endpoints, INodeLocator locator, IReconnectPolicyFactory reconnectPolicyFactory, NodeFactory nodeFactory)
				: base(endpoints, locator, reconnectPolicyFactory)
			{
				this.nodeFactory = nodeFactory;
			}

			protected override INode CreateNode(IPEndPoint endpoint) => nodeFactory.Create(this, endpoint);
		}

		private abstract class NodeFactory
		{
			public abstract INode Create(ClusterBase owner, IPEndPoint endpoint);
		}

		private class WorkingNodeFactory : NodeFactory
		{
			public override INode Create(ClusterBase owner, IPEndPoint endpoint) => new MockNode(owner, endpoint, new MockSocket(), new MockFailurePolicyFactory());
		}

		private class FailNodeFactory : NodeFactory
		{
			public override INode Create(ClusterBase owner, IPEndPoint endpoint) => new FailNode(owner, endpoint, new MockSocket(), new MockFailurePolicyFactory());

			private class FailNode : NodeBase
			{
				public FailNode(ICluster owner, IPEndPoint endpoint, ISocket socket, IFailurePolicyFactory failurePolicyFactory) : base(owner, endpoint, socket, failurePolicyFactory)
				{
				}

				public override void Connect(CancellationToken token)
				{
					base.Connect(token);
					IsAlive = false;
				}

				protected override IResponse CreateResponse() => throw new NotImplementedException();
			}
		}

		private class MockFailurePolicyFactory : IFailurePolicyFactory
		{
			public IFailurePolicy Create(INode arg) => new MockFailurePolicy();

			private class MockFailurePolicy : IFailurePolicy
			{
				public void Reset() { }
				public bool ShouldFail() => true;
			}
		}

		private class ImmediateReconnectPolicyFactory : IReconnectPolicyFactory
		{
			public IReconnectPolicy Create(INode arg) => new ImmediateRP();

			private class ImmediateRP : IReconnectPolicy
			{
				public void Reset() { }
				public TimeSpan Schedule() => TimeSpan.Zero;
			}
		}

		private class MockSocket : ISocket
		{
			public bool IsAlive => true;
			public bool IsReceiving => false;
			public bool IsSending => false;
			public int RequestBufferSize { get; set; }
			public int ResponseBufferSize { get; set; }
			public TimeSpan ConnectionTimeout { get; set; }
			public TimeSpan RequestTimeout { get; set; }
			public TimeSpan ResponseTimeout { get; set; }
			public ReadBuffer ResponseBuffer { get; } = new ReadBuffer(MemoryPool<byte>.Shared.Rent(100).Memory);
			public WriteBuffer RequestBuffer { get; } = new WriteBuffer(MemoryPool<byte>.Shared.Rent(100).Memory);

			public void Connect(IPEndPoint endpoint, CancellationToken token) { }
			public void Dispose() { }

			public bool ReceiveResponse(Action<bool> whenDone)
			{
				whenDone(true);

				return false;
			}

			public bool SendRequest(Action<bool> whenDone)
			{
				whenDone(true);

				return false;
			}
		}

		private class MockNode : NodeBase
		{
			public MockNode(ICluster owner, IPEndPoint endpoint, ISocket socket, IFailurePolicyFactory failurePolicyFactory)
				: base(owner, endpoint, socket, failurePolicyFactory)
			{
			}

			protected override IResponse CreateResponse() => new MockResponse();
		}

		private class FailingOperation : IItemOperation
		{
			public const string ExceptionMessage = "itsa mock";

			public ReadOnlyMemory<byte> Key => new byte[] { 1, 2, 3, 4 };

			public void Failed(Exception e) { }
			public bool Handles(IResponse response) => true;

			public IRequest CreateRequest() => new MockRequest();
			public bool ProcessResponse(IResponse response) => throw new NotImplementedException(ExceptionMessage);
		}

		private class WorkingOperation : IOperation
		{
			public IRequest CreateRequest() => new MockRequest();
			public void Failed(Exception e) { }
			public bool Handles(IResponse response) => true;
			public bool ProcessResponse(IResponse response) => false;
		}

		private class MockRequest : IRequest
		{
			public void Dispose() { }

			public bool WriteTo(WriteBuffer buffer)
			{
				buffer.Advance(1);
				return false;
			}
		}

		private class MockResponse : IResponse
		{
			public void Dispose() { }
			public bool Read(ReadBuffer stream) => false;
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
