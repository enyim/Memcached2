using System;
using System.Buffers;
using System.Diagnostics;

namespace Enyim.Caching.Memcached
{
	internal class SequenceCopier
	{
		private readonly ReadOnlySequence<byte> source;
		private readonly WriteBuffer target;

		private int position;
		private ReadOnlyMemory<byte> currentMemory;
		private SequencePosition nextPosition;

		public SequenceCopier(in ReadOnlySequence<byte> source, WriteBuffer target)
		{
			this.source = source;
			this.target = target;
			position = -1;

			currentMemory = default;
			nextPosition = source.Start;
		}

		public bool Copy()
		{
			while (true)
			{
				if (position == -1)
				{
					if (nextPosition.GetObject() == null)
					{
						return false;
					}

					if (!source.TryGet(ref nextPosition, out currentMemory))
						return false;

					position = 0;
				}

				position += target.TryAppend(currentMemory.Span.Slice(position));

				// could not append the whole slice => target buffer is full
				if (position < currentMemory.Length) return true;

				Debug.Assert(position == currentMemory.Length);

				position = -1;
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
