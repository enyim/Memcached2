using System;
using System.Linq;
using System.Collections.Generic;

namespace Enyim.Caching
{
	public class SocketOptions : ISocketOptions
	{
		public TimeSpan ConnectionTimeout { get; set; } = AsyncSocket.Defaults.ConnectionTimeoutMsec;
		public TimeSpan RequestTimeout { get; set; } = AsyncSocket.Defaults.SendTimeoutMsec;
		public TimeSpan ResponseTimeout { get; set; } = AsyncSocket.Defaults.ReceiveTimeoutMsec;
		public int RequestBufferSize { get; set; } = AsyncSocket.Defaults.SendBufferSize;
		public int ResponseBufferSize { get; set; } = AsyncSocket.Defaults.ReceiveBufferSize;
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
