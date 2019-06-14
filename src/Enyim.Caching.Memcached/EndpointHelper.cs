using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Enyim.Caching.Memcached
{
	public static class EndPointHelper
	{
		public static IPEndPoint ParseEndPoint(string value, int port = 0)
		{
			if (String.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(value));

			var index = value.LastIndexOf(':');
			if (index == -1)
			{
				if (port < 1)
					throw new ArgumentException($"Invalid endpoint '{value}' - host:port is expected", nameof(value));
			}
			else if (!Int32.TryParse(value.Substring(index + 1), out port))
			{
				throw new ArgumentException("Cannot parse " + value, nameof(value));
			}

			var hostName = index == -1 ? value : value.Remove(index);
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
					throw new ArgumentException("Expected IPV4 adress but received " + value);
			}

			return new IPEndPoint(address, port);
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
