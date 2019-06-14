using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

namespace Enyim.Caching.Memcached
{
	public abstract class MemcachedClientTestBase<TFixture> : TestBase, IClassFixture<TFixture>
		where TFixture : class, IClusterFixture
	{
		protected MemcachedClientTestBase(TFixture fixture)
		{
			Fixture = fixture;
			Client = fixture.CreateClient();
		}

		protected TFixture Fixture { get; }
		protected MemcachedClient Client { get; }

		protected async Task WithNewItem(Func<string, string, Task> assert, [CallerMemberName] string caller = null)
		{
			await WithNewItem(GetRandomString(), assert, caller);
		}

		protected async Task WithNewItem<T>(T value, Func<string, T, Task> assert, [CallerMemberName] string caller = null)
		{
			var key = GetUniqueKey(caller ?? GetRandomString());

			await Client.SetAsync(key, value);
			await assert(key, value);
		}

		protected async Task WithNewItems(int count, Func<string[], string, Task> assert, [CallerMemberName] string caller = null)
		{
			await WithNewItems(GetRandomString(), count, assert, caller);
		}

		protected async Task WithNewItems<T>(T value, int count, Func<string[], T, Task> assert, [CallerMemberName] string caller = null)
		{
			var keyPrefix = GetUniqueKey(caller ?? GetRandomString()) + "_";
			var tasks = new List<Task>();
			var keys = new List<string>();

			for (var i = 0; i < count; i++)
			{
				var key = keyPrefix + i;
				keys.Add(key);
				tasks.Add(Client.SetAsync(key, value));
			}

			await Task.WhenAll(tasks);
			await assert(keys.ToArray(), value);
		}

		protected void AssertSuccess(OperationResult result, bool hasCas = true, string message = null)
		{
			Assert.True(result.Success, message);
			Assert.Null(result.Exception);
			Assert.Equal((OperationStatus)Protocol.Status.Success, result.StatusCode);

			if (hasCas)
			{
				Assert.True(result.Cas > 0, "expected Cas value, but result does not have it set");
			}
			else
			{
				Assert.True(result.Cas == 0, "result should not have Cas value");
			}
		}

		protected void AssertFail(OperationResult result, int expectedStatusCode = -1)
		{
			Assert.False(result.Success);
			Assert.Null(result.Exception);

			if (expectedStatusCode > -1)
			{
				Assert.Equal((OperationStatus)expectedStatusCode, result.StatusCode);
			}
			else
			{
				Assert.True(result.StatusCode > 0, "StatusCode must be non-zero for failures");
			}
		}

		protected void AssertSuccess<T>(OperationResult<T> result)
		{
			Assert.True(result.Success);
			Assert.True(result.Cas > 0, "expected Cas value, but result does not have it set");
			Assert.Null(result.Exception);
			Assert.Equal(OperationStatus.Success, result.StatusCode);
		}

		protected void AssertFail<T>(OperationResult<T> result, int statusCode)
		{
			Assert.False(result.Success);
			Assert.Null(result.Exception);
			Assert.Equal((OperationStatus)statusCode, result.StatusCode);
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
