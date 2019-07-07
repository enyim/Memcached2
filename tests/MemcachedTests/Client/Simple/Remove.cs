using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Enyim.Caching.Memcached.Client
{
	public class RemoveAsync : MemcachedClientTestBase<TheTestFixture>
	{
		public RemoveAsync(TheTestFixture fixture) : base(fixture) { }

		[Fact]
		public async Task When_Removing_A_Valid_Key_Result_Is_Successful()
		{
			await WithNewItem(async (key, _) =>
			{
				Assert.True(await Client.DeleteAsync(key));
				Assert.Null(await Client.GetAsync<object>(key));
			});
		}

		[Fact]
		public async Task When_Removing_An_Invalid_Key_Result_Is_Not_Successful()
		{
			var key = GetUniqueKey("Remove_Invalid");

			Assert.Null(await Client.GetAsync<object>(key)); // sanity-check
			Assert.False(await Client.DeleteAsync(key));
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
