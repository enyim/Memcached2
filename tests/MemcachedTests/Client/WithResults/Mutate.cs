using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Enyim.Caching.Memcached.Client
{
	public class MutateWithResultAsync : MemcachedClientTestBase<EverythingShared>
	{
		public MutateWithResultAsync(EverythingShared fixture) : base(fixture) { }

		private void AssertEqual<T>(T expected, OperationResult<T> actual)
		{
			AssertSuccess(actual);
			Assert.Equal(expected, actual.Value);
		}

		[Fact]
		public async Task When_Incrementing_Value_Result_Is_Successful()
		{
			var key = GetUniqueKey("Increment");

			AssertEqual(200ul, await Client.MutateWithResultAsync(MutationMode.Increment, key, 10, 200));
			AssertEqual(210ul, await Client.MutateWithResultAsync(MutationMode.Increment, key, 10, 200));
		}

		[Fact]
		public async Task When_Getting_An_Incremented_Value_It_Must_Be_A_String()
		{
			var key = GetUniqueKey("Increment_Get");

			AssertEqual(200ul, await Client.MutateWithResultAsync(MutationMode.Increment, key, 10, 200));
			AssertEqual(210ul, await Client.MutateWithResultAsync(MutationMode.Increment, key, 10, 200));
			AssertEqual("210", await Client.GetWithResultAsync<string>(key));
		}

		[Fact]
		public async Task Can_Increment_Value_Initialized_As_String()
		{
			await WithNewItem("200", async (key, _) =>
			{
				AssertEqual(210ul, await Client.MutateWithResultAsync(MutationMode.Increment, key, 10, 10));
				AssertEqual("210", await Client.GetWithResultAsync<string>(key));
			});
		}

		[Fact]
		public async Task Can_Increment_Value_With_Cas()
		{
			await WithNewItem("200", async (key, _) =>
			{
				var tmp = await Client.GetWithResultAsync<string>(key);
				AssertSuccess(tmp);

				AssertEqual(210ul, await Client.MutateWithResultAsync(MutationMode.Increment, key, 10, 10, tmp.Cas));
			});
		}

		[Fact]
		public async Task When_Decrementing_Value_Result_Is_Successful()
		{
			var key = GetUniqueKey("Decrement");

			AssertEqual(200ul, await Client.MutateWithResultAsync(MutationMode.Decrement, key, 10, 200));
			AssertEqual(190ul, await Client.MutateWithResultAsync(MutationMode.Decrement, key, 10, 200));
		}

		[Fact]
		public async Task When_Getting_A_Decremented_Value_It_Must_Be_A_String()
		{
			var key = GetUniqueKey("Decrement_Get");

			AssertEqual(200ul, await Client.MutateWithResultAsync(MutationMode.Decrement, key, 10, 200));
			AssertEqual(190ul, await Client.MutateWithResultAsync(MutationMode.Decrement, key, 10, 200));
			AssertEqual("190", await Client.GetWithResultAsync<string>(key));
		}

		[Fact]
		public async Task Can_Decrement_Value_Initialized_As_String()
		{
			await WithNewItem("200", async (key, _) =>
			{
				AssertEqual(190ul, await Client.MutateWithResultAsync(MutationMode.Decrement, key, 10, 10));
				AssertEqual("190", await Client.GetWithResultAsync<string>(key));
			});
		}

		[Fact]
		public async Task Can_Decrement_Value_With_Cas()
		{
			await WithNewItem("200", async (key, _) =>
			{
				var tmp = await Client.GetWithResultAsync<string>(key);
				AssertSuccess(tmp);

				AssertEqual(190ul, await Client.MutateWithResultAsync(MutationMode.Decrement, key, 10, 10, tmp.Cas));
			});
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
