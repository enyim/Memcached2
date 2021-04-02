using System;
using System.Text;

#if (NETSTANDARD2_0 || NET471 || NET472 || NET48)
using Caching;
#endif

namespace Enyim.Caching.Memcached
{
	public sealed class Utf8KeyFormatter : IKeyFormatter
	{
		private static readonly UTF8Encoding utf8 = new UTF8Encoding(false);

		public void Serialize(SequenceBuilder target, string key)
		{
			KeyFormatter.ThrowIfInvalidKey(key);

			var keyLength = utf8.GetByteCount(key);
			var buffer = target.Request(keyLength).Span;

			utf8.GetBytes(key, buffer);
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
