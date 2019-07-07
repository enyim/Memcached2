using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Xunit;

namespace Enyim.Caching.Memcached
{
	public class Utf8KeyFormatterTests
	{
		[Fact]
		public void Utf8KeyFormatter_Should_Throw_When_Input_Is_Empty()
		{
			var transformer = new Utf8KeyFormatter();

			Assert.Throws<ArgumentNullException>(() => transformer.Serialize(new SequenceBuilder(MemoryPool<byte>.Shared), null));
			Assert.Throws<ArgumentException>(() => transformer.Serialize(new SequenceBuilder(MemoryPool<byte>.Shared), ""));
		}

		[Theory]
		[InlineData("Hello World")]
		[InlineData("1234567890")]
		[InlineData("árvíztűrőtükörfúrógép")]
		[InlineData("日本語版テストデータ")]
		public void KeyFormatterTests(string input)
		{
			var transformer = new Utf8KeyFormatter();
			using var builder = new SequenceBuilder(MemoryPool<byte>.Shared);
			transformer.Serialize(builder, input);

			var expected = new UTF8Encoding(false).GetBytes(input);

			Assert.Equal(expected, builder.Commit().ToArray());
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
