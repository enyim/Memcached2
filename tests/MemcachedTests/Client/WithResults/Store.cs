using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Enyim.Caching.Memcached.Client
{
	public class StoreWithResultAsync : MemcachedClientTestBase<TheTestFixture>
	{
		public StoreWithResultAsync(TheTestFixture fixture) : base(fixture) { }

		[Fact]
		public async Task When_Storing_Item_With_New_Key_And_StoreMode_Add_Result_Is_Successful()
		{
			AssertSuccess(await Client.StoreWithResultAsync(StoreMode.Add, GetUniqueKey("Add_Once"), GetRandomString()));
		}

		[Fact]
		public async Task When_Storing_Item_With_Existing_Key_And_StoreMode_Add_Result_Is_Not_Successful()
		{
			var key = GetUniqueKey("Add_Twice");

			AssertSuccess(await Client.StoreWithResultAsync(StoreMode.Add, key, GetRandomString()));
			AssertFail(await Client.StoreWithResultAsync(StoreMode.Add, key, GetRandomString()), Protocol.Status.KeyExists);
		}

		[Fact]
		public async Task When_Storing_Item_With_New_Key_And_StoreMode_Replace_Result_Is_Not_Successful()
		{
			AssertFail(await Client.StoreWithResultAsync(StoreMode.Replace, key: GetUniqueKey("New_Replace"), GetRandomString()), Protocol.Status.KeyNotFound);
		}

		[Fact]
		public async Task When_Storing_Item_With_New_Key_And_StoreMode_Set_Result_Is_Successful()
		{
			AssertSuccess(await Client.StoreWithResultAsync(StoreMode.Set, key: GetUniqueKey("New_Set"), GetRandomString()));
		}

		[Fact]
		public async Task When_Storing_Item_With_Existing_Key_And_StoreMode_Replace_Result_Is_Successful()
		{
			var key = GetUniqueKey("Existing_Replace");

			AssertSuccess(await Client.StoreWithResultAsync(StoreMode.Add, key, GetRandomString()));
			AssertSuccess(await Client.StoreWithResultAsync(StoreMode.Replace, key, GetRandomString()));
		}

		[Fact]
		public async Task When_Storing_Item_With_Existing_Key_And_StoreMode_Set_Result_Is_Successful()
		{
			var key = GetUniqueKey("Existing_Set");

			AssertSuccess(await Client.StoreWithResultAsync(StoreMode.Add, key, GetRandomString()));
			AssertSuccess(await Client.StoreWithResultAsync(StoreMode.Set, key, GetRandomString()));
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
