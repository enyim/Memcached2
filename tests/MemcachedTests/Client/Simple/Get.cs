using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

namespace Enyim.Caching.Memcached.Client
{
	public class GetAsync : MemcachedClientTestBase<EverythingShared>
	{
		public GetAsync(EverythingShared fixture) : base(fixture) { }

		[Fact]
		public async Task Can_Read_Items_Larger_Than_Receive_Buffer()
		{
			var value = new byte[AsyncSocket.Defaults.ReceiveBufferSize + 17];

			value[0] = 100;
			value[value.Length / 2] = 100;
			value[^1] = 100;

			await WithNewItem(value, async (key, value) =>
			{
				var result = await Client.GetAsync<object>(key) as byte[];

				Assert.NotNull(result);
				Assert.Equal(result.Length, value.Length);
				Assert.Equal(value.AsEnumerable(), result.AsEnumerable());
			});
		}

		[Fact]
		public async Task When_Getting_Existing_Item_Value_Is_Not_Null_And_Result_Is_Successful()
		{
			await WithNewItem(async (key, value) => Assert.Equal(value, await Client.GetAsync<object>(key)));
		}

		[Fact]
		public async Task When_Getting_Item_For_Invalid_Key_Result_Is_Default_Or_Null()
		{
			Assert.Null(await Client.GetAsync<object>(GetUniqueKey("Get_Invalid")));
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
