using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Enyim.Caching.Memcached
{
	public class MemcachedClusterOptions : IMemcachedClusterOptions
	{
		private MemoryPool<byte> allocator = MemoryPool<byte>.Shared;
		private INodeLocator locator = new DefaultNodeLocator();
		private ISocketFactory socketFactory = new AsyncSocketFactory();
		private IFailurePolicyFactory failurePolicyFactory = new ImmediateFailurePolicyFactory();
		private IReconnectPolicyFactory reconnectPolicyFactory = new PeriodicReconnectPolicyFactory();

		public MemoryPool<byte> Allocator
		{
			get => allocator;
			set => allocator = value ?? throw PropertyCannotBeNull();
		}

		public INodeLocator Locator
		{
			get => locator;
			set => locator = value ?? throw PropertyCannotBeNull();
		}

		public ISocketFactory SocketFactory
		{
			get => socketFactory;
			set => socketFactory = value ?? throw PropertyCannotBeNull();
		}

		public IFailurePolicyFactory FailurePolicyFactory
		{
			get => failurePolicyFactory;
			set => failurePolicyFactory = value ?? throw PropertyCannotBeNull();
		}

		public IReconnectPolicyFactory ReconnectPolicyFactory
		{
			get => reconnectPolicyFactory;
			set => reconnectPolicyFactory = value ?? throw PropertyCannotBeNull();
		}

		private static ArgumentNullException PropertyCannotBeNull([CallerMemberName] string? property = null)
			=> new ArgumentNullException("value", $"Property {property} cannot be null");
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
