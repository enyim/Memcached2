using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

namespace Enyim.Caching.Memcached
{
	public partial class MemcachedClientTests
	{
#if ENABLE_MULTIGET
		[Fact]
		public async Task When_Getting_Multiple_Keys_As_Object_Without_Cas_Result_Is_Successful()
		{
			await SetupNewItems(100, async (keys, value) =>
			{
				var values = await client.Get(keys);

				Assert.Equal(keys.OrderBy(a => a), values.Keys.OrderBy(a => a));
				Assert.All(values, kvp => Assert.Equal(value, kvp.Value));
			});
		}
#endif
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
