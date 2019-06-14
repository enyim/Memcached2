using System;

namespace Enyim.Caching.Memcached
{
	internal static class SequenceBuilderExtensions
	{
		public static void Append(this SequenceBuilder self, in ReadOnlyMemory<byte> data)
		{
			if (data.Length > 0)
			{
				var tmp = self.Request(data.Length, advance: true);
				data.Span.CopyTo(tmp.Span);
			}
		}

		public static void Append(this SequenceBuilder self, in ReadOnlySpan<byte> data)
		{
			if (data.Length > 0)
			{
				var tmp = self.Request(data.Length, advance: true);
				data.CopyTo(tmp.Span);
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
