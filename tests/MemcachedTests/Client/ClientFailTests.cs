using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;
using Moq;

namespace Enyim.Caching.Memcached
{
	public partial class ClientFailTests : TestBase, IClassFixture<SharedLocalServersFixture>
	{
		private readonly SharedLocalServersFixture fixture;

		public ClientFailTests(SharedLocalServersFixture fixture)
		{
			this.fixture = fixture;
		}

		private ICluster GetCustomCluster()
		{
			// operations being tested in this class are expected to fail early without bringing their node down
			// the mockInstance will throw if one of the nodes become dead
			var mockFactory = new Mock<IFailurePolicyFactory>();
			var mockInstance = new Mock<IFailurePolicy>(MockBehavior.Strict);

			mockFactory.Setup(f => f.Create(It.IsAny<INode>())).Returns(mockInstance.Object);

			var retval = new MemcachedCluster(fixture.Run(), new MemcachedClusterOptions { FailurePolicyFactory = mockFactory.Object });
			retval.Start();

			return retval;
		}

		[Fact]
		public async Task Should_Throw_ArgumentException_When_Key_Is_Too_Long()
		{
			using var cluster = GetCustomCluster();
			var client = new MemcachedClient(cluster);

			var exception = await Assert.ThrowsAsync<ArgumentException>(() => client.SetAsync(new string('a', 65536), "aaaa"));

			Assert.StartsWith("Key is too long; was 65536, maximum is", exception.Message);
		}

		[Fact]
		public async Task Should_Throw_SerializationException_When_TranscoderFails()
		{
			using var cluster = GetCustomCluster();
			var client = new MemcachedClient(cluster);

			await Assert.ThrowsAsync<SerializationException>(() => client.SetAsync("aaaaa", new { A = "a" }));
		}

		[Fact]
		public void Client_Should_Only_Work_With_Started_Server()
		{
			var mockCluster = new Mock<ICluster>();
			mockCluster.SetupGet(c => c.IsStarted).Returns(false);

			var e = Assert.Throws<ArgumentException>(() => new MemcachedClient(mockCluster.Object, new MemcachedClientOptions()));
			Assert.Contains("must be started", e.Message);

			Mock.Verify();
		}

		[Fact]
		public void Constructor_Should_Throw_When_Options_Is_Invalid()
		{
			static Exception AssertException(Action<Mock<IMemcachedClientOptions>> extraSetup)
			{
				var defaultOptions = new MemcachedClientOptions();
				var mockCluster = new Mock<ICluster>();
				mockCluster.SetupGet(c => c.IsStarted).Returns(true);

				var mockOptions = new Mock<IMemcachedClientOptions>(MockBehavior.Strict);

				mockOptions.SetupGet(o => o.Allocator).Returns(defaultOptions.Allocator);
				mockOptions.SetupGet(o => o.KeyTransformer).Returns(defaultOptions.KeyTransformer);
				mockOptions.SetupGet(o => o.Transcoder).Returns(defaultOptions.Transcoder);

				extraSetup?.Invoke(mockOptions);

				return Assert.Throws<ArgumentNullException>(() => new MemcachedClient(mockCluster.Object, mockOptions.Object));
			}

			Exception e;

			e = AssertException(mock => mock.SetupGet(o => o.Allocator).Returns(() => null));
			Assert.Contains(nameof(IMemcachedClientOptions.Allocator), e.Message);

			e = AssertException(mock => mock.SetupGet(o => o.KeyTransformer).Returns(() => null));
			Assert.Contains(nameof(IMemcachedClientOptions.KeyTransformer), e.Message);

			e = AssertException(mock => mock.SetupGet(o => o.Transcoder).Returns(() => null));
			Assert.Contains(nameof(IMemcachedClientOptions.Transcoder), e.Message);

			Mock.Verify();
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
