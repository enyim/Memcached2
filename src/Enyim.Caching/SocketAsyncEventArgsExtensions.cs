using System;
using System.Collections.Generic;
using System.Text;

namespace System.Net.Sockets
{
	public static class SocketAsyncEventArgsExtensions
	{
#if NETSTANDARD2_0
		public static void SetBuffer(this SocketAsyncEventArgs args, Memory<byte> bytes)
		{
			if (args == null) throw new ArgumentNullException(nameof(args));
			args.SetBuffer(bytes.ToArray(), 0, bytes.Length);
		}
#endif
	}
}
