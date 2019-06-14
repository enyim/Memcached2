using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Moq;

namespace Enyim.Caching.Memcached.Client
{
	public partial class MemcachedClientExtensionsTests
	{
		private const string Key = "MockKey";
		private const ulong MutateDelta = 16;
		private const ulong MutateDefaultValue = 8;
		private static readonly Expiration HasExpiration = new Expiration(1234);
		private static readonly byte[] Data = new byte[] { 1, 2, 3, 4 };
		private static readonly object Value = new Object();

		private void Verify<TResult>(Action<IMemcachedClient> operation, Expression<Func<IMemcachedClient, TResult>> expectation)
		{
			var mock = new Mock<IMemcachedClient>();

			operation(mock.Object);
			mock.Verify(expectation);
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
