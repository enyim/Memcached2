using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Enyim.Caching.Memcached.Internal
{
	public static class EndPointHelper
	{
		public static IPEndPoint Parse(string address)
		{
			if (String.IsNullOrEmpty(address)) throw new ArgumentNullException(nameof(address));

			var index = address.LastIndexOf(':');

			if (index == -1)
				return New(address, Protocol.DefaultPort);

			if (index == 0)
				throw new ArgumentException($"Invalid endpoint '{address}' - host:port is expected", nameof(address));

			if (!Int32.TryParse(address.Substring(index + 1), out var port))
				throw new ArgumentException($"Invalid port number in '{address}'", nameof(address));

			return New(address.Remove(index), port);
		}

		public static IPEndPoint New(string hostName, int port)
		{
			if (String.IsNullOrEmpty(hostName)) throw new ArgumentNullException(nameof(hostName));
			if (port < 1) throw new ArgumentOutOfRangeException(nameof(port), port, $"Invalid port '{port}'");

			static bool IsIPV4(IPAddress a) => a.AddressFamily == AddressFamily.InterNetwork;

			if (!IPAddress.TryParse(hostName, out var address))
			{
				address = Array.Find(Dns.GetHostEntry(hostName).AddressList, IsIPV4)
								?? throw new ArgumentException($"Could not resolve host '{hostName}'");
			}
			else
			{
				// TODO test ipv6
				if (!IsIPV4(address))
					throw new ArgumentException("Expected IPV4 adress but received " + hostName);
			}

			return new IPEndPoint(address, port);
		}

		public static IPEndPoint[] ParseList(IEnumerable<string> value)
			=> ParseList((value ?? throw new ArgumentNullException(nameof(value))).ToArray());

		public static IPEndPoint[] ParseList(params string[] values)
			=> (values ?? throw new ArgumentNullException(nameof(values)))
					.Where(v => !String.IsNullOrEmpty(v))
					.SelectMany(ParseInlineEndPoints)
					.ToArray();

		private static IEnumerable<IPEndPoint> ParseInlineEndPoints(string value)
			=> value
				.Split(new char[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
				.Where(v => !String.IsNullOrWhiteSpace(v))
				.Select(v => Parse(v.Trim()));
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
