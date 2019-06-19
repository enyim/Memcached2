using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

namespace Enyim.Caching.Memcached.Client
{
	public class GetWithResultAsync : MemcachedClientTestBase<TheTestFixture>
	{
		public GetWithResultAsync(TheTestFixture fixture) : base(fixture) { }

		[Fact]
		public async Task Can_Read_Items_Larger_Than_Receive_Buffer()
		{
			var data = new byte[AsyncSocket.Defaults.ReceiveBufferSize + 17];
			new Random().NextBytes(data);

			await WithNewItem(data, async (key, original) =>
			{
				var result = await Client.GetWithResultAsync<byte[]>(key);

				AssertSuccess(result);

				Assert.Equal(original, result.Value);
				Assert.NotSame(original, result.Value);
			});
		}

		[Fact]
		public async Task When_Getting_Existing_Item_Value_Is_Not_Null_And_Result_Is_Successful()
		{
			await WithNewItem(async (key, value) =>
			{
				var result = await Client.GetWithResultAsync<string>(key);

				AssertSuccess(result);
				Assert.Equal(value, result.Value);
			});
		}

		[Fact]
		public async Task When_Getting_Item_For_Invalid_Key_Result_Is_Default_Or_Null()
		{
			var result = await Client.GetWithResultAsync<object>(GetUniqueKey("Get_Invalid"));

			AssertFail(result, Protocol.Status.KeyNotFound);
			Assert.Null(result.Value);
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
