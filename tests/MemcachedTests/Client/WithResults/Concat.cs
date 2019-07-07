using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Enyim.Caching.Memcached.Client
{
	public class ConcatWithResultAsync : MemcachedClientTestBase<TheTestFixture>
	{
		public ConcatWithResultAsync(TheTestFixture fixture) : base(fixture) { }

		[Fact]
		public async Task When_Appending_To_Existing_Value_Result_Is_Successful()
		{
			await WithNewItem(async (key, value) =>
			{
				const string ToAppend = "The End";

				var result = await Client.ConcatWithResultAsync(ConcatenationMode.Append, key, Encoding.UTF8.GetBytes(ToAppend).AsMemory(), 0);

				AssertSuccess(result);

				Assert.Equal(value + ToAppend, await Client.GetAsync<string>(key));
			});
		}

		[Fact]
		public async Task When_Appending_To_Existing_Value_With_Valid_Cas_Result_Is_Successful()
		{
			await WithNewItem(async (key, value) =>
			{
				const string ToAppend = "The End";

				var item = await Client.GetWithResultAsync<string>(key);
				var result = await Client.ConcatWithResultAsync(ConcatenationMode.Append, key, Encoding.UTF8.GetBytes(ToAppend).AsMemory(), item.Cas);

				AssertSuccess(result);

				Assert.Equal(value + ToAppend, await Client.GetAsync<string>(key));
			});
		}


		[Fact]
		public async Task When_Appending_To_Existing_Value_With_Invalid_Cas_Result_Is_Unsuccessful()
		{
			await WithNewItem(async (key, value) =>
			{
				const string ToAppend = "The End";

				var result = await Client.ConcatWithResultAsync(ConcatenationMode.Append, key, Encoding.UTF8.GetBytes(ToAppend).AsMemory(), UInt64.MaxValue - 1);

				AssertFail(result);
			});
		}

		[Fact]
		public async Task When_Appending_To_Invalid_Key_Result_Is_Not_Successful()
		{
			const string ToAppend = "The End";
			var key = GetUniqueKey("Append_Fail");

			var result = await Client.ConcatWithResultAsync(ConcatenationMode.Append, key, Encoding.UTF8.GetBytes(ToAppend).AsMemory());

			AssertFail(result, Protocol.Status.ItemNotStored);
			Assert.Null(await Client.GetAsync<object>(key));
		}

		[Fact]
		public async Task When_Prepending_To_Existing_Value_Result_Is_Successful()
		{
			await WithNewItem(async (key, value) =>
			{
				const string ToPrepend = "The Beginning";

				var result = await Client.ConcatWithResultAsync(ConcatenationMode.Prepend, key, Encoding.UTF8.GetBytes(ToPrepend).AsMemory(), 0);

				AssertSuccess(result);

				Assert.Equal(ToPrepend + value, await Client.GetAsync<string>(key));
			});
		}

		[Fact]
		public async Task When_Prepending_To_Existing_Value_With_Valid_Cas_Result_Is_Successful()
		{
			await WithNewItem(async (key, value) =>
			{
				const string ToPrepend = "The Beginning";

				var item = await Client.GetWithResultAsync<string>(key);
				var result = await Client.ConcatWithResultAsync(ConcatenationMode.Prepend, key, Encoding.UTF8.GetBytes(ToPrepend).AsMemory(), item.Cas);

				AssertSuccess(result);

				Assert.Equal(ToPrepend + value, await Client.GetAsync<string>(key));
			});
		}

		[Fact]
		public async Task When_Prepending_To_Existing_Value_With_Invalid_Cas_Result_Is_Unsuccessful()
		{
			await WithNewItem(async (key, value) =>
			{
				const string ToPrepend = "The Beginning";

				var result = await Client.ConcatWithResultAsync(ConcatenationMode.Prepend, key, Encoding.UTF8.GetBytes(ToPrepend).AsMemory(), UInt64.MaxValue - 1);

				AssertFail(result);
			});
		}

		[Fact]
		public async Task When_Prepending_To_Invalid_Key_Result_Is_Not_Successful()
		{
			const string ToPrepend = "The End";
			var key = GetUniqueKey("Prepend_Fail");

			var result = await Client.ConcatWithResultAsync(ConcatenationMode.Prepend, key, Encoding.UTF8.GetBytes(ToPrepend).AsMemory());

			AssertFail(result, Protocol.Status.ItemNotStored);
			Assert.Null(await Client.GetAsync<object>(key));
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
