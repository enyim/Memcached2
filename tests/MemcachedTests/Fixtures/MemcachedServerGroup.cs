using System;
using System.Buffers;
using System.Linq;
using System.Net;
using System.Threading;

namespace Enyim.Caching.Memcached
{
	// starts the specified number of memched servers
	public class MemcachedServerGroup : IDisposable
	{
		private readonly int serverCount;
		private MemcachedServer[] servers;

		public MemcachedServerGroup(int serverCount)
		{
			this.serverCount = serverCount;
		}

		public IPEndPoint[] Run()
		{
			if (servers != null) throw new InvalidOperationException("Already started");

			servers = new MemcachedServer[serverCount];

			try
			{
				for (var i = 0; i < servers.Length; i++)
				{
					const bool Verbose =
#if DEBUG
						true;
#else
						false;
#endif

					servers[i] = MemcachedServer.WithAutoPort(verbose: Verbose, hidden: !Verbose);
					servers[i].Start();
				}

				Thread.Sleep(2000); // wait fopr the servers to start

				return servers.Select(server => new IPEndPoint(IPAddress.Loopback, server.Port)).ToArray();
			}
			catch
			{
				Dispose();

				throw;
			}
		}

		public void Dispose()
		{
			if (servers != null)
			{
				foreach (var s in servers)
					s.Dispose();

				servers = null;
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
