using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Xunit;

namespace Enyim.Caching.Memcached
{
	public class Utf8KeyTransformerTests
	{
		[Theory]
		[MemberData(nameof(DataForKeyTransformerTests))]
		public void KeyTransformerTests(string input)
		{
			var transformer = new Utf8KeyTransformer(MemoryPool<byte>.Shared);

			using (var key = transformer.Transform(input))
			{
				var utf8 = new UTF8Encoding(false).GetBytes(input);
				Assert.Equal(utf8, key.Memory.ToArray());
			}
		}

		private static object[] R(params object[] args) => args;

		public static IEnumerable<object[]> DataForKeyTransformerTests
		{
			get
			{
				yield return R("Hello World");
				yield return R("1234");
				yield return R("1234 1234");
				yield return R("{0}");
				yield return R("Hello {2} World");
				yield return R("Hello {2,1234} World");
				yield return R("Hello {2:yy-mm} World");
				yield return R("{0:yy-mm} Hello {2:yy-mm} World {3:abcd,1234} world");
				yield return R("őúűéá,.-ÓÜÖŰÚŐÁÉ_:?|&íÍ");
			}
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
