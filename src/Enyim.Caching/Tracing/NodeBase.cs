#pragma warning disable RCS1163, RCS1057, IDE0060
using System;
using System.Diagnostics.Tracing;

namespace Enyim.Caching
{
	[EventSource(Name = "Enyim-Caching-Node")]
	internal static class NodeEventSource
	{
		[Event(1, Message = "Node.Run started for {0}", Keywords = Keywords.Run)]
		public static void RunStart(string name) { }
		[Event(2, Message = "Node.Run finished for {0}", Keywords = Keywords.Run)]
		public static void RunStop(string name) { }

		[Event(3, Message = "Write operation enqueued for {0}", Keywords = Keywords.OpQueue)]
		public static void EnqueueWriteOp(string name) { }
		[Event(4, Message = "Write operation dequeued for {0}", Keywords = Keywords.OpQueue)]
		public static void DequeueWriteOp(string name) { }

		[Event(5, Message = "Read operation enqueued for {0}", Keywords = Keywords.OpQueue)]
		public static void EnqueueReadOp(string name) { }
		[Event(6, Message = "Read operation dequeued for {0}", Keywords = Keywords.OpQueue)]
		public static void DequeueReadOp(string name) { }

		[Event(7, Message = "Error while enqueueing for {0}", Keywords = Keywords.OpQueue)]
		public static void NodeError(string name) { }

		public static class Keywords
		{
			public const EventKeywords Run = (EventKeywords)1;
			public const EventKeywords OpQueue = (EventKeywords)2;
		}
	}
}

#pragma warning restore RCS1163, RCS1057, IDE0060

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
