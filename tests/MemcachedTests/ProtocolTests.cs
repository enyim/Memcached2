using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Enyim.Caching.Memcached
{
	public class ProtocolTests
	{
		[Theory]
		[MemberData(nameof(DataForValidateIsSilent))]
		public void ValidateIsSilent(OpCode input, bool isSilent)
		{
			Assert.Equal(isSilent, Protocol.IsSilent(input));
		}

		[Theory]
		[MemberData(nameof(DataForValidateToSilent))]
		public void ValidateToSilent(OpCode input, OpCode silent)
		{
			Assert.Equal(silent, Protocol.ToSilent(input));
		}

		private static object[] R(params object[] args) => args;
		private static object[] N(OpCode o, OpCode p) => new object[] { o, p, false };
		//private static object[] S(OpCode o) => args;

		public static IEnumerable<object[]> DataForValidateIsSilent
		{
			get
			{
				return Enum
						.GetValues(typeof(OpCode))
						.Cast<OpCode>()
						.Select(op => new object[]
						{
							op,
							op.ToString().EndsWith("Q")
						});
			}
		}

		public static IEnumerable<object[]> DataForValidateToSilent
		{
			get
			{
				var values = Enum.GetValues(typeof(OpCode))
									.Cast<OpCode>()
									.ToDictionary(o => o.ToString(), o => (int)o);

				foreach (var kvp in values)
				{
					var (name, op) = (kvp.Key, kvp.Value);

					if (!name.EndsWith("Q")
						&& values.TryGetValue(name + "Q", out var silent))
					{
						yield return new object[]
						{
								(OpCode)op,
								(OpCode)silent
						};
					}
					else
					{
						yield return new object[]
						{
							(OpCode)op,
							(OpCode)op
						};
					}
				}
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
