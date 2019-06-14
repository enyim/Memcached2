using System;
using System.Linq;
using Xunit;

namespace Enyim.Caching.Memcached
{
	public abstract class TestBase
	{
		private readonly string name;
		private readonly Random random;

		protected TestBase()
		{
			this.name = GetType().Name + "_";
			this.random = new Random();
		}

		protected TestBase(string name)
		{
			this.name = name + "_";
			this.random = new Random();
		}

		protected string GetUniqueKey(string prefix = null)
			=> (!String.IsNullOrEmpty(prefix) ? prefix + "_" : "")
				+ name
				+ DateTime.Now.Ticks;

		protected string[] GetUniqueKeys(string prefix = null, int max = 5)
		 => Enumerable
				.Range(0, max)
				.Select(i => GetUniqueKey(prefix) + "_" + i)
				.ToArray();

		protected string GetRandomString() => name + "_random_value_" + random.Next();

		//public static void IfThrows<T>(IOperationResult result) where T : Exception
		//{
		//	Assert.False(result.Success);
		//	Assert.IsType<T>(result.Exception);
		//}
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
