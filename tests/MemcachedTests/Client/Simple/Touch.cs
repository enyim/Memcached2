using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Enyim.Caching.Memcached.Client
{
	public class TouchAsync : MemcachedClientTestBase<TheTestFixture>
	{
		public TouchAsync(TheTestFixture fixture) : base(fixture) { }

		public const int WaitButStillAlive = 500;
		public const int WaitUntilExpires = 5000;

		static readonly Expiration DefaultExpiration = new Expiration(3);
		static readonly Expiration NewExpiration = new Expiration(20);

		[Fact, Trait("slow", "yes")]
		public async Task When_Getting_An_Expired_Item_It_Should_Be_Null()
		{
			var key = GetUniqueKey("Get_Expired");
			var value = GetRandomString();

			Assert.True(await Client.StoreAsync(StoreMode.Set, key, value, DefaultExpiration), "item not stored");
			Thread.Sleep(WaitButStillAlive);
			Assert.Equal(value, await Client.GetAsync<string>(key));

			Thread.Sleep(WaitUntilExpires);
			Assert.Null(await Client.GetAsync<string>(key));
		}

		[Fact, Trait("slow", "yes")]
		public async Task When_Getting_And_Touching_An_Item_It_Should_Not_Expire()
		{
			var key = GetUniqueKey("Get_And_Touch");
			var value = GetRandomString();

			Assert.True(await Client.StoreAsync(StoreMode.Set, key, value, DefaultExpiration), "item not stored");
			Thread.Sleep(WaitButStillAlive);
			Assert.Equal(value, await Client.GetAndTouchAsync<string>(key, NewExpiration));

			Thread.Sleep(WaitUntilExpires);
			Assert.Equal(value, await Client.GetAsync<string>(key));
		}

		[Fact, Trait("slow", "yes")]
		public async Task When_Touching_An_Item_It_Should_Not_Expire()
		{
			var key = GetUniqueKey("Touch");
			var value = GetRandomString();

			Assert.True(await Client.StoreAsync(StoreMode.Set, key, value, DefaultExpiration), "item not stored");
			Thread.Sleep(WaitButStillAlive);
			Assert.True(await Client.TouchAsync(key, NewExpiration), "touch failed");

			Thread.Sleep(WaitUntilExpires);
			Assert.Equal(value, await Client.GetAsync<string>(key));
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
