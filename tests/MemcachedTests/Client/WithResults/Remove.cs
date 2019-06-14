using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Enyim.Caching.Memcached.Client
{
	public class RemoveWithResultAsync : MemcachedClientTestBase<EverythingShared>
	{
		public RemoveWithResultAsync(EverythingShared fixture) : base(fixture) { }

		[Fact]
		public async Task When_Removing_A_Valid_Key_Result_Is_Successful()
		{
			await WithNewItem(async (key, value) =>
			{
				// deleted items will not return a cas value (doh)
				AssertSuccess(await Client.DeleteWithResultAsync(key), hasCas: false);
				AssertFail(await Client.GetWithResultAsync<object>(key), Protocol.Status.KeyNotFound);
			});
		}

		[Fact]
		public async Task When_Removing_A_Valid_Key_With_Cas_Result_Is_Successful()
		{
			await WithNewItem(async (key, value) =>
			{
				var tmp = await Client.GetWithResultAsync<string>(key);

				// deleted items will not return a cas value (doh)
				AssertSuccess(await Client.DeleteWithResultAsync(key, tmp.Cas), hasCas: false);

				AssertFail(await Client.GetWithResultAsync<string>(key), Protocol.Status.KeyNotFound);
			});
		}

		[Fact]
		public async Task When_Removing_A_Valid_Key_With_Invalid_Cas_Result_Is_Unsuccessful()
		{
			await WithNewItem(async (key, value) =>
			{
				var tmp = await Client.GetWithResultAsync<string>(key);

				AssertFail(await Client.DeleteWithResultAsync(key, tmp.Cas - 1));
				AssertSuccess(await Client.GetWithResultAsync<string>(key));
			});
		}

		[Fact]
		public async Task When_Removing_An_Invalid_Key_Result_Is_Not_Successful()
		{
			var key = GetUniqueKey("Remove_Invalid");

			AssertFail(await Client.GetWithResultAsync<object>(key), Protocol.Status.KeyNotFound); // sanity-check
			AssertFail(await Client.DeleteWithResultAsync(key), Protocol.Status.KeyNotFound);
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
