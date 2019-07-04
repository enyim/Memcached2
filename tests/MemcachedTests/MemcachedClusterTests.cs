using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Moq;
using System.Linq.Expressions;

namespace Enyim.Caching.Memcached
{
	public class MemcachedClusterTests
	{
		[Fact]
		public void Constructor_Should_Throw_When_Options_Is_Invalid()
		{
			static Exception AssertException(Action<Mock<IMemcachedClusterOptions>> extraSetup)
			{
				var defaultOptions = new MemcachedClusterOptions();
				var mockOptions = new Mock<IMemcachedClusterOptions>(MockBehavior.Strict);

				mockOptions.SetupGet(o => o.Allocator).Returns(defaultOptions.Allocator);
				mockOptions.SetupGet(o => o.FailurePolicyFactory).Returns(defaultOptions.FailurePolicyFactory);
				mockOptions.SetupGet(o => o.Locator).Returns(defaultOptions.Locator);
				mockOptions.SetupGet(o => o.ReconnectPolicyFactory).Returns(defaultOptions.ReconnectPolicyFactory);
				mockOptions.SetupGet(o => o.SocketFactory).Returns(defaultOptions.SocketFactory);

				extraSetup?.Invoke(mockOptions);

				return Assert.Throws<ArgumentNullException>(() => new MemcachedCluster("localhost", mockOptions.Object));
			}

			Exception e;

			e = Assert.Throws<ArgumentException>(() => new MemcachedCluster(Enumerable.Empty<IPEndPoint>(), new MemcachedClusterOptions()));
			Assert.Contains("Must provide at least one endpoint", e.Message);

			e = AssertException(mock => mock.SetupGet(o => o.Allocator).Returns(() => null));
			Assert.Contains(nameof(IMemcachedClusterOptions.Allocator), e.Message);

			e = AssertException(mock => mock.SetupGet(o => o.FailurePolicyFactory).Returns(() => null));
			Assert.Contains(nameof(IMemcachedClusterOptions.FailurePolicyFactory), e.Message);

			e = AssertException(mock => mock.SetupGet(o => o.Locator).Returns(() => null));
			Assert.Contains(nameof(IMemcachedClusterOptions.Locator), e.Message);

			e = AssertException(mock => mock.SetupGet(o => o.ReconnectPolicyFactory).Returns(() => null));
			Assert.Contains(nameof(IMemcachedClusterOptions.ReconnectPolicyFactory), e.Message);

			e = AssertException(mock => mock.SetupGet(o => o.SocketFactory).Returns(() => null));
			Assert.Contains(nameof(IMemcachedClusterOptions.SocketFactory), e.Message);

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
