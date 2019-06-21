using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Enyim.Caching.Memcached
{
	public static class Protocol
	{
		public const int DefaultPort = 11211;

		public const byte RequestMagic = 0x80;
		public const byte ResponseMagic = 0x81;
		public const int HeaderLength = 24;

		public const ushort MaxKeyLength = UInt16.MaxValue;
		public const int MaxExtraLength = 0xff;

		public static class Request
		{
			internal const int HEADER_INDEX_MAGIC = 0;
			internal const int HEADER_INDEX_OPCODE = 1;
			internal const int HEADER_INDEX_KEY = 2; // 2-3
			internal const int HEADER_INDEX_EXTRA = 4;
			internal const int HEADER_INDEX_DATATYPE = 5;
			internal const int HEADER_INDEX_VBUCKET = 6; // 6-7
			internal const int HEADER_INDEX_BODY_LENGTH = 8; // 8-11
			internal const int HEADER_INDEX_OPAQUE = 12; // 12-15
			internal const int HEADER_INDEX_CAS = 16; // 16-23
		}

		public static class Response
		{
			internal const int HEADER_INDEX_MAGIC = 0;
			internal const int HEADER_INDEX_OPCODE = 1;
			internal const int HEADER_INDEX_KEY = 2; // 2-3
			internal const int HEADER_INDEX_EXTRA = 4;
			internal const int HEADER_INDEX_DATATYPE = 5;
			internal const int HEADER_INDEX_STATUS = 6; // 6-7
			internal const int HEADER_INDEX_BODY_LENGTH = 8; // 8-11
			internal const int HEADER_INDEX_OPAQUE = 12; // 12-15
			internal const int HEADER_INDEX_CAS = 16; // 16-23
		}

		public static class Status
		{
			public const int Success = 0;
			public const int KeyNotFound = 0x0001;
			public const int KeyExists = 0x0002;
			public const int ValueTooLarge = 0x0003;
			public const int InvalidArguments = 0x0004;
			public const int ItemNotStored = 0x0005;
			public const int IncrDecrNonNumericValue = 0x0006;
			public const int NotMyVBucket = 0x0007;
			public const int AuthenticationError = 0x0008;
			public const int AuthenticationContinue = 0x0009;
			public const int UnknownCommand = 0x0081;
			public const int OutOfMemory = 0x0082;
			public const int NotSupported = 0x0083;
			public const int InternalError = 0x0084;
			public const int Busy = 0x0085;
			public const int TemporaryFailure = 0x0086;
		}

		public const ulong NO_CAS = 0;
		public const ulong MUTATE_DEFAULT_DELTA = 1;
		public const ulong MUTATE_DEFAULT_VALUE = 1;

		private static readonly bool[] SilentOps;
		private static readonly OpCode[] NormalToSilent;

		static Protocol()
		{
			var values = Enum.GetValues(typeof(OpCode)).Cast<OpCode>().ToDictionary(o => o.ToString(), o => (int)o);
			SilentOps = new bool[values.Values.Max() + 1];
			NormalToSilent = new OpCode[SilentOps.Length];

#pragma warning disable CS8619
			foreach (var (name, op) in values)
			{
				SilentOps[op] = name.EndsWith("Q");
				NormalToSilent[op] = (OpCode)(!name.EndsWith("Q") && values.TryGetValue(name + "Q", out var silent)
										? silent
										: op);
			}
#pragma warning enable CS8619
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static OpCode ToSilent(byte opcode) => NormalToSilent[opcode];

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static OpCode ToSilent(OpCode opcode) => NormalToSilent[(int)opcode];

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsSilent(byte opcode) => SilentOps[opcode];

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsSilent(OpCode opcode) => SilentOps[(int)opcode];
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
