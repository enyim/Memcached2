#pragma warning disable RCS1163, RCS1057, IDE0060
using System;
using System.Diagnostics.Tracing;
using System.Net.Sockets;

namespace Enyim.Caching
{
	[EventSource(Name = "Enyim-Caching-AsyncSocket")]
	internal static class AsyncSocketEventSource
	{
		[Event(1, Level = EventLevel.Informational, Keywords = Keywords.Connect, Message = "connect start")]
		public static void ConnectStart(string endpoint) { }

		[Event(2, Level = EventLevel.Informational, Keywords = Keywords.Connect, Message = "ConnectStop")]
		public static void ConnectStop(string endpoint) { }

		[Event(3, Level = EventLevel.Error, Keywords = Keywords.Connect, Message = "ConnectFail")]
		public static void ConnectFail(string endpoint, SocketError status) { }

		[Event(4, Level = EventLevel.Verbose, Keywords = Keywords.Send, Message = "SendStart")]
		public static void SendStart(string endpoint, bool isAlive, int byteCount) { }

		[Event(5, Level = EventLevel.Verbose, Keywords = Keywords.Send, Message = "SendStop")]
		public static void SendStop(string endpoint, bool isAlive, bool success) { }

		[Event(6, Level = EventLevel.Verbose, Keywords = Keywords.Send, Message = "SendChunk")]
		public static void SendChunk(string endpoint, bool isAlive, int bytesSent, SocketError status) { }

		[Event(7, Level = EventLevel.Verbose, Keywords = Keywords.Receive, Message = "ReceiveStart")]
		public static void ReceiveStart(string endpoint, bool isAlive) { }

		[Event(8, Level = EventLevel.Verbose, Keywords = Keywords.Receive, Message = "ReceiveStop")]
		public static void ReceiveStop(string endpoint, bool isAlive, bool success) { }

		[Event(9, Level = EventLevel.Verbose, Keywords = Keywords.Receive, Message = "ReceiveChunk")]
		public static void ReceiveChunk(string endpoint, bool isAlive, int bytesReceived, SocketError status) { }

		public static partial class Keywords
		{
			public const EventKeywords Connect = (EventKeywords)0b0000_0001;
			public const EventKeywords Send = (EventKeywords)0b0000_0010;
			public const EventKeywords Receive = (EventKeywords)0b0000_0100;
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
