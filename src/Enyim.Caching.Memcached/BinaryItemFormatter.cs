using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

#if (NETSTANDARD2_0 || NET471 || NET472 || NET48)
using Caching;
#endif

namespace Enyim.Caching.Memcached
{
	public class BinaryItemFormatter : IItemFormatter
	{
		private const byte TRUE = 1;
		private const byte FALSE = 0;

		public const uint FlagPrefix = 0x10fa5200;
		public const uint RawDataFlag = FlagPrefix | 0xaa;

		private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

		public uint Serialize(SequenceBuilder output, object? value)
		{
			try
			{
				return DoSerialize(output, value);
			}
			catch (Exception e) when (!(e is SerializationException))
			{
				throw new SerializationException($"Exception '{e.Message}' while serializing '{value}'. See inner exception for details.", e);
			}
		}

		public object? Deserialize(ReadOnlyMemory<byte> data, uint flags)
		{
			try
			{
				return DoDeserialize(data, flags);
			}
			catch (Exception e) when (!(e is SerializationException))
			{
				throw new SerializationException($"Exception '{e.Message}' while deserializing data. See inner exception for details.", e);
			}
		}

		private uint DoSerialize(SequenceBuilder output, object? value)
		{
			if (value is byte[] tmpByteArray)
			{
#pragma warning disable IDE0067 // nothing to dispose
				new SegmentedStream(output).Write(tmpByteArray, 0, tmpByteArray.Length);
#pragma warning restore IDE0067

				return RawDataFlag;
			}

			TypeCode code;

			if (value == null)
			{
				code = TypeCode.DBNull;
			}
			else
			{
				code = Type.GetTypeCode(value.GetType());

#pragma warning disable IDE0049 // readability

				switch (code)
				{
					case TypeCode.Empty:
					case TypeCode.DBNull: break;

					case TypeCode.Object: new BinaryFormatter().Serialize(new SegmentedStream(output), value); break;
					case TypeCode.String:
						var theString = (string)value;
						if (theString.Length > 0)
							Utf8NoBom.GetBytes(theString.AsSpan(), output.Request(Utf8NoBom.GetByteCount(theString)).Span);
						break;

					case TypeCode.SByte: output.Request(sizeof(SByte)).Span[0] = (Byte)(SByte)value; break;
					case TypeCode.Byte: output.Request(sizeof(Byte)).Span[0] = (Byte)value; break;
					case TypeCode.Boolean: output.Request(sizeof(Byte)).Span[0] = (Boolean)value ? TRUE : FALSE; break;

					case TypeCode.Char: BinaryPrimitives.WriteUInt16LittleEndian(output.Request(sizeof(Char)).Span, (Char)value); break;

					case TypeCode.Int16: BinaryPrimitives.WriteInt16LittleEndian(output.Request(sizeof(Int16)).Span, (Int16)value); break;
					case TypeCode.Int32: BinaryPrimitives.WriteInt32LittleEndian(output.Request(sizeof(Int32)).Span, (Int32)value); break;
					case TypeCode.Int64: BinaryPrimitives.WriteInt64LittleEndian(output.Request(sizeof(Int64)).Span, (Int64)value); break;

					case TypeCode.UInt16: BinaryPrimitives.WriteUInt16LittleEndian(output.Request(sizeof(UInt16)).Span, (UInt16)value); break;
					case TypeCode.UInt32: BinaryPrimitives.WriteUInt32LittleEndian(output.Request(sizeof(UInt32)).Span, (UInt32)value); break;
					case TypeCode.UInt64: BinaryPrimitives.WriteUInt64LittleEndian(output.Request(sizeof(UInt64)).Span, (UInt64)value); break;

					case TypeCode.DateTime: BinaryPrimitives.WriteInt64LittleEndian(output.Request(sizeof(Int64)).Span, ((DateTime)value).ToBinary()); break;

					case TypeCode.Single:
#if !NETSTANDARD2_0
						BitConverter.TryWriteBytes(output.Request(sizeof(Single)).Span, (Single)value);
#else
						var float_bytes = BitConverter.GetBytes((Single)value);
						output.Append(float_bytes.AsSpan());
#endif
						break;
					case TypeCode.Double:
#if !NETSTANDARD2_0
						BitConverter.TryWriteBytes(output.Request(sizeof(Single)).Span, (Single)value);
#else
						var double_bytes = BitConverter.GetBytes((Double)value);
						output.Append(double_bytes.AsSpan());
#endif

						break;
					case TypeCode.Decimal:
						var src = Decimal.GetBits((Decimal)value);
						var target = output.Request(sizeof(Int32) * 4).Span;

						BinaryPrimitives.WriteInt32LittleEndian(target, src[0]);
						BinaryPrimitives.WriteInt32LittleEndian(target.Slice(sizeof(Int32)), src[1]);
						BinaryPrimitives.WriteInt32LittleEndian(target.Slice(sizeof(Int32) + sizeof(Int32)), src[2]);
						BinaryPrimitives.WriteInt32LittleEndian(target.Slice(sizeof(Int32) + sizeof(Int32) + sizeof(Int32)), src[3]);

						break;

					default:
						throw new SerializationException($"Cannot serialize '{value}', unknown TypeCode: {code}");
				}

#pragma warning restore IDE0049
			}

			return FlagPrefix | (uint)code;
		}

		private object? DoDeserialize(ReadOnlyMemory<byte> data, uint flags)
		{
			if (flags == RawDataFlag
				// check if unknown flag
				|| ((flags & (FlagPrefix + 0xff)) != flags))
				return data.ToArray();

			var code = (TypeCode)(flags & 0xff);
			var span = data.Span;

#pragma warning disable IDE0049 // readability

			switch (code)
			{
				case TypeCode.Object:
					if (!MemoryMarshal.TryGetArray(data, out var segment))
						throw new SerializationException("Cannot deserialize object, MemoryMarshal was not able to get the byte[] of the buffer."); // TODO dump the first 16-32 bytes into the exception

					using (var ms = new MemoryStream(segment.Array, segment.Offset, segment.Count, false))
						return new BinaryFormatter().Deserialize(ms);

				case TypeCode.DBNull: return null;

				// incrementing a non-existing key then getting it
				// returns as a string, but the flag will be 0
				// so treat all 0 flagged items as string
				// this may help inter-client data management as well
				case TypeCode.Empty:
				case TypeCode.String: return Utf8NoBom.GetString(span);

				case TypeCode.SByte: return (SByte)span[0];
				case TypeCode.Byte: return span[0];
				case TypeCode.Boolean: return span[0] != FALSE;

				case TypeCode.Char: return (char)BinaryPrimitives.ReadUInt16LittleEndian(span);

				case TypeCode.Int16: return BinaryPrimitives.ReadInt16LittleEndian(span);
				case TypeCode.Int32: return BinaryPrimitives.ReadInt32LittleEndian(span);
				case TypeCode.Int64: return BinaryPrimitives.ReadInt64LittleEndian(span);

				case TypeCode.UInt16: return BinaryPrimitives.ReadUInt16LittleEndian(span);
				case TypeCode.UInt32: return BinaryPrimitives.ReadUInt32LittleEndian(span);
				case TypeCode.UInt64: return BinaryPrimitives.ReadUInt64LittleEndian(span);

				case TypeCode.DateTime: return DateTime.FromBinary(BinaryPrimitives.ReadInt64LittleEndian(span));

				case TypeCode.Single:
#if !NETSTANDARD2_0
					return BitConverter.ToSingle(span);
#else
					return BitConverter.ToSingle(span.ToArray(),0);
#endif
				case TypeCode.Double:
#if !NETSTANDARD2_0
					return BitConverter.ToDouble(span);
#else
					return BitConverter.ToDouble(span.ToArray(), 0);
#endif
				case TypeCode.Decimal:

					var bits = new int[4];
					bits[0] = BinaryPrimitives.ReadInt32LittleEndian(span);
					bits[1] = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(sizeof(Int32)));
					bits[2] = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(sizeof(Int32) + sizeof(Int32)));
					bits[3] = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(sizeof(Int32) + sizeof(Int32) + sizeof(Int32)));

					return new Decimal(bits);
			}
#pragma warning restore IDE0049

			return data.ToArray();
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
