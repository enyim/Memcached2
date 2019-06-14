using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Enyim.Caching.Memcached.Client
{
	public partial class MemcachedClientExtensionsTests
	{
		[Fact]
		public void ReplaceAsync_With_Defaults()
		{
			Verify(c => c.ReplaceAsync(Key, Value),
					c => c.StoreAsync(StoreMode.Replace, Key, Value, Expiration.Never));
		}

		[Fact]
		public void ReplaceAsync_With_Custom_Expiration()
		{
			Verify(c => c.ReplaceAsync(Key, Value, HasExpiration),
					c => c.StoreAsync(StoreMode.Replace, Key, Value, HasExpiration));
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
