using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Enyim.Caching.Memcached.Client
{
	public class MutateAsync : MemcachedClientTestBase<TheTestFixture>
	{
		public MutateAsync(TheTestFixture fixture) : base(fixture) { }

		[Fact]
		public async Task When_Incrementing_Value_Result_Is_Successful()
		{
			var key = GetUniqueKey("Increment");

			Assert.Equal(200ul, await Client.MutateAsync(MutationMode.Increment, key, 10, 200));
			Assert.Equal(210ul, await Client.MutateAsync(MutationMode.Increment, key, 10, 200));
		}

		[Fact]
		public async Task When_Getting_An_Incremented_Value_It_Must_Be_A_String()
		{
			var key = GetUniqueKey("Increment_Get");

			Assert.Equal(200ul, await Client.MutateAsync(MutationMode.Increment, key, 10, 200));
			Assert.Equal(210ul, await Client.MutateAsync(MutationMode.Increment, key, 10, 200));
			Assert.Equal("210", await Client.GetAsync<string>(key));
		}

		[Fact]
		public async Task Can_Increment_Value_Initialized_By_Store()
		{
			var key = GetUniqueKey("Increment_Store");

			Assert.True(await Client.StoreAsync(StoreMode.Set, key, "200", Expiration.Never));
			Assert.Equal(210ul, await Client.MutateAsync(MutationMode.Increment, key, 10, 10));
			Assert.Equal("210", await Client.GetAsync<string>(key));
		}

		[Fact]
		public async Task When_Decrementing_Value_Result_Is_Successful()
		{
			var key = GetUniqueKey("Decrement");

			Assert.Equal(200ul, await Client.MutateAsync(MutationMode.Decrement, key, 10, 200));
			Assert.Equal(190ul, await Client.MutateAsync(MutationMode.Decrement, key, 10, 200));
		}

		[Fact]
		public async Task When_Getting_A_Decremented_Value_It_Must_Be_A_String()
		{
			var key = GetUniqueKey("Decrement_Get");

			Assert.Equal(200ul, await Client.MutateAsync(MutationMode.Decrement, key, 10, 200));
			Assert.Equal(190ul, await Client.MutateAsync(MutationMode.Decrement, key, 10, 200));
			Assert.Equal("190", await Client.GetAsync<string>(key));
		}

		[Fact]
		public async Task Can_Decrement_Value_Initialized_By_Store()
		{
			var key = GetUniqueKey("Decrement_Store");

			Assert.True(await Client.StoreAsync(StoreMode.Set, key, "200", Expiration.Never));
			Assert.Equal(190ul, await Client.MutateAsync(MutationMode.Decrement, key, 10, 10));
			Assert.Equal("190", await Client.GetAsync<string>(key));
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
