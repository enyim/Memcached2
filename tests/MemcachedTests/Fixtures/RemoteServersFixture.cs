using System;
using System.Linq;
using System.Net;

namespace Enyim.Caching.Memcached
{
	public class RemoteServersFixture : IServerFixture
	{
		public IPEndPoint[] Run()
		{
			var host = IPAddress.Parse("10.211.55.10");
			var ports = new[] { 12000, 12001, 12002, 12003 };

			return ports.Select(p => new IPEndPoint(host, p)).ToArray();
		}

		void IDisposable.Dispose() { }
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
