using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using Xunit;

namespace Enyim.Caching.Memcached
{
	public class BinaryTranscoderTests
	{
		[Theory]
		[MemberData(nameof(DataForSerialization))]
		public void Can_Serialize(object value)
		{
			JustSerialize(value, out var flag);

			if (value is byte[])
			{
				Assert.Equal(BinaryTranscoder.RawDataFlag, flag);
			}
			else
			{
				Assert.Equal(BinaryTranscoder.FlagPrefix, flag & BinaryTranscoder.FlagPrefix);
				Assert.Equal(value == null ? TypeCode.DBNull : Type.GetTypeCode(value.GetType()), (TypeCode)(flag & 0xff));
			}
		}

		[Theory]
		[MemberData(nameof(DataForSerialization))]
		public void Can_Roundtrip_Deserialize(object value)
		{
			var source = JustSerialize(value, out var flag).AsMemory();
			var roundtripValue = new BinaryTranscoder().Deserialize(source, flag);

			Assert.Equal(value, roundtripValue);
		}

		[Fact]
		public void Object_Not_Marked_As_Serializable_Must_Throw_SerializationException()
		{
			var e = Assert.Throws<SerializationException>(() => JustSerialize(new { A = 1, b = 2 }, out _));

			Assert.IsType<System.Runtime.Serialization.SerializationException>(e.InnerException);
		}

		[Fact]
		public void Object_Marked_As_Serializable_Should_Be_Serialized_Properly()
		{
			JustSerialize(new ToSerialize(), out _);
		}

		[Serializable]
		class ToSerialize
		{
			public int MyProperty { get; set; }
		}

		private byte[] JustSerialize(object value, out uint flag)
		{
			using var builder = new SequenceBuilder(MemoryPool<byte>.Shared);
			var t = new BinaryTranscoder();
			flag = t.Serialize(builder, value);

			return builder.Commit().ToArray();
		}

		private static object[] R(params object[] args) => args;

		public static IEnumerable<object[]> DataForSerialization
		{
			get
			{
				return new[]
				{
					new object[] { null },
					R(true),
					R(Byte.MaxValue),
					R(SByte.MaxValue),
					R(UInt16.MaxValue),
					R(UInt32.MaxValue),
					R(UInt64.MaxValue),
					R(Int16.MaxValue),
					R(Int32.MaxValue),
					R(Int64.MaxValue),
					R(Single.MaxValue),
					R(Double.MaxValue),
					R(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }),
					R("Hello World")
				};
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
