using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Enyim.Caching
{
	public class SequenceBuilderTests
	{
		[Fact]
		public void TestBuilder()
		{
			var expected = Enumerable.Range(0, 3000).Select(i => (byte)i).ToArray();
			var sb = ToBuilder(expected);
			var seq = sb.Commit().ToArray();

			Assert.Equal(expected, seq);
		}

		[Theory]
		[MemberData(nameof(DataForAppendSequences))]
		public void AppendSequences(IEnumerable<byte>[] parts)
		{
			var expected = new List<byte>();
			var sbs = parts.Select(part =>
			{
				var tmp = part.ToArray();
				expected.AddRange(tmp);

				return ToBuilder(tmp);
			}).ToArray();

			var first = sbs.First();

			foreach (var sb in sbs.Skip(1))
				first.Append(sb);


			var seq = first.Commit();

			Assert.Equal(expected, seq.ToArray());
		}

		public static IEnumerable<object[]> DataForAppendSequences
		{
			get
			{
				yield return new object[]
				{
					new IEnumerable<byte>[]
					{
						Enumerable.Range(0, 3000).Select(i => (byte)i),
						Enumerable.Range(0, 200).Select(i => (byte)(255 - i))
					}
				};

				yield return new object[]
				{
					new IEnumerable<byte>[]
					{
						Enumerable.Empty<byte>(),
						Enumerable.Range(0, 200).Select(i => (byte)(255 - i))
					}
				};

				yield return new object[]
				{
					new IEnumerable<byte>[]
					{
						Enumerable.Range(0, 3000).Select(i => (byte)i),
						Enumerable.Empty<byte>()
					}
				};

				yield return new object[]
				{
					new IEnumerable<byte>[]
					{
						Enumerable.Empty<byte>(),
						Enumerable.Empty<byte>()
					}
				};

				yield return new object[]
				{
					new IEnumerable<byte>[]
					{
						Enumerable.Range(0, 3000).Select(i => (byte)i),
						Enumerable.Empty<byte>(),
						Enumerable.Empty<byte>(),
						Enumerable.Range(0, 200).Select(i => (byte)(255 - i))
					}
				};

				yield return new object[]
				{
					new IEnumerable<byte>[]
					{
						Enumerable.Range(0, 20).Select(i => (byte)(255 - i)),
						Enumerable.Range(0, 40).Select(i => (byte)(255 - i)),
						Enumerable.Range(0, 60).Select(i => (byte)(255 - i)),
						Enumerable.Range(0, 80).Select(i => (byte)(255 - i)),
						Enumerable.Range(0, 100).Select(i => (byte)(255 - i))
					}
				};
			}
		}

		private static SequenceBuilder ToBuilder(IEnumerable<byte> data)
		{
			var sb = new SequenceBuilder(MemoryPool<byte>.Shared);

			foreach (var b in data)
			{
				var m = sb.Request(1);
				m.Span[0] = b;
			}

			return sb;
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
