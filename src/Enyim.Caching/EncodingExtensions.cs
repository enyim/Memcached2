using System;
using System.Net.Sockets;
using System.Text;

namespace Caching
{
	public static class EncodingExtensions
	{
#if NETSTANDARD2_0
		public static int GetBytes<T>(this T encoding, string chars, Span<byte> bytes)
			where T : Encoding
		{
			var stringBytes = encoding.GetBytes(chars);
			stringBytes.AsSpan().CopyTo(bytes);

			return stringBytes.Length;
		}

		public static byte[] GetBytes<T>(this T encoding, ReadOnlySpan<char> chars)
			where T : Encoding
		{
			return encoding.GetBytes(chars.ToString());
		}

		public static int GetBytes<T>(this T encoding, ReadOnlySpan<char> chars, Span<byte> bytes)
			where T : Encoding
		{
			var stringBytes = encoding.GetBytes(chars);
			stringBytes.AsSpan().CopyTo(bytes);

			return stringBytes.Length;
		}

		public static string GetString<T>(this T encoding, ReadOnlySpan<byte> bytes)
			where T : Encoding =>
			encoding.GetString(bytes.ToArray());
#endif
	}
}
