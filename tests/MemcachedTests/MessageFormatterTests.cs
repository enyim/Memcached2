using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Xunit;

namespace Enyim.Diagnostics
{
	public class MessageFormatterTests
	{
		[Theory]
		[MemberData(nameof(DataForFormatMessageTemplate))]
		public void FormatMessageTemplate(string input, string expected)
		{
			Assert.Equal(expected, Enyim.Diagnostics.ConsoleLoggerFactory.FormatMessageTemplate(input));
		}

		private static object[] Same(object arg) => new[] { arg, arg };
		private static object[] R(string input, string expected) => new[] { input, expected };

		public static IEnumerable<object[]> DataForFormatMessageTemplate
		{
			get
			{
				yield return Same("Hello World");
				yield return Same("1234");
				yield return Same("1234 1234");
				yield return Same("{0}");
				yield return Same("Hello {2} World");
				yield return Same("Hello {2,1234} World");
				yield return Same("Hello {2:yy-mm} World");
				yield return Same("{0:yy-mm} Hello {2:yy-mm} World {3:abcd,1234} world");

				yield return R("{Hello} {World}!", "<<{0}>> <<{1}>>!");
				yield return R("{Hello:1234} {World,abcd}!", "<<{0:1234}>> <<{1,abcd}>>!");
				yield return R("{Hello:1234} {1234} {World,abcd}!", "<<{0:1234}>> {1234} <<{1,abcd}>>!");
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
