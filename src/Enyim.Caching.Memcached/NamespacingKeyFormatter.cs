using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Enyim.Caching.Memcached
{
	public sealed class NamespacingKeyFormatter : IKeyFormatter
	{
		private static readonly UTF8Encoding utf8 = new UTF8Encoding(false);

		private readonly Memory<byte> prefix;

		public NamespacingKeyFormatter(string @namespace)
		{
			prefix = utf8.GetBytes(@namespace ?? throw new ArgumentNullException(nameof(@namespace))).AsMemory();
		}

		public void Serialize(SequenceBuilder target, string key)
		{
			KeyFormatter.ThrowIfInvalidKey(key);

			var keyLength = prefix.Length + utf8.GetByteCount(key);
			var buffer = target.Request(keyLength).Span;

			prefix.Span.CopyTo(buffer);
			utf8.GetBytes(key, buffer.Slice(prefix.Length));
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
