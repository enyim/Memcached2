using System;
using System.Linq;
using System.Collections.Generic;

namespace Enyim.Caching
{
	public class AsyncSocketFactory : ISocketFactory
	{
		private readonly LoHMemoryPool lohPool = new LoHMemoryPool();

		private readonly TimeSpan connectionTimeout;
		private readonly TimeSpan requestTimeout;
		private readonly TimeSpan responseTimeout;
		private readonly int requestBufferSize;
		private readonly int responseBufferSize;

		public AsyncSocketFactory(ISocketOptions? options = null)
		{
			// make a snapshot of the options
			// TODO review this
			if (options == null) options = new SocketOptions();

			connectionTimeout = options.ConnectionTimeout;
			requestTimeout = options.RequestTimeout;
			responseTimeout = options.ResponseTimeout;
			requestBufferSize = options.RequestBufferSize;
			responseBufferSize = options.ResponseBufferSize;
		}

		public ISocket Create() => new AsyncSocket(lohPool)
		{
			ConnectionTimeout = connectionTimeout,
			RequestTimeout = requestTimeout,
			ResponseTimeout = responseTimeout,
			RequestBufferSize = requestBufferSize,
			ResponseBufferSize = responseBufferSize
		};
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
