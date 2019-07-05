using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using Enyim.Caching.Memcached.Internal;
using Xunit;

namespace Enyim.Caching.Memcached
{
	public class EndpointHelperTests
	{
		[Theory]
		[MemberData(nameof(DataFoParse))]
		public void Parse(string value, IPEndPoint expected)
		{
			var ep = EndPointHelper.Parse(value);

			Assert.Equal(expected, ep);
		}

		[Theory]
		[MemberData(nameof(DataForNew))]
		public void New(string host, int port, IPEndPoint expected)
		{
			var ep = EndPointHelper.New(host, port);

			Assert.Equal(expected, ep);
		}

		[Fact]
		public void ParseFail()
		{
			Assert.Throws<ArgumentNullException>(() => EndPointHelper.Parse(null));
			Assert.Throws<ArgumentNullException>(() => EndPointHelper.Parse(""));
			Assert.Throws<ArgumentException>(() => EndPointHelper.Parse("host:"));
			Assert.Throws<ArgumentException>(() => EndPointHelper.Parse(":1234"));
			Assert.Throws<ArgumentOutOfRangeException>(() => EndPointHelper.Parse("abcd:-100"));
			Assert.Throws<ArgumentOutOfRangeException>(() => EndPointHelper.Parse("abcd:0"));
		}

		[Fact]
		public void NewFail()
		{
			Assert.Throws<ArgumentNullException>(() => EndPointHelper.New(null, 1234));
			Assert.Throws<ArgumentNullException>(() => EndPointHelper.New("", 1234));
			Assert.Throws<ArgumentOutOfRangeException>(() => EndPointHelper.New("abcd", -1));
		}

		private static object[] R(params object[] args) => args;

		public static IEnumerable<object[]> DataFoParse
		{
			get
			{
				return new[]
				{
					R("localhost", new IPEndPoint(IPAddress.Loopback, Protocol.DefaultPort)),
					R("localhost:1234", new IPEndPoint(IPAddress.Loopback, 1234)),
					R("example.com:1234", new IPEndPoint(IPAddress.Parse("93.184.216.34"), 1234))
				};
			}
		}

		public static IEnumerable<object[]> DataForNew
		{
			get
			{
				return new[]
				{
					R("localhost", 1234, new IPEndPoint(IPAddress.Loopback, 1234)),
					R("example.com",1234, new IPEndPoint(IPAddress.Parse("93.184.216.34"), 1234))
				};
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
