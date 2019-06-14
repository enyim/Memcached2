using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Enyim.Caching
{
	public class SegmentedStream : Stream
	{
		private readonly SequenceBuilder builder;

		public SegmentedStream(SequenceBuilder builder)
		{
			this.builder = builder;
		}

		public override long Length => builder.Length;
		public override bool CanWrite => true;

		public override void WriteByte(byte value) => builder.Request(1).Span[0] = value;
		public override void Write(byte[] buffer, int offset, int count) => new Span<byte>(buffer, offset, count).CopyTo(builder.Request(count).Span);
		public override void Write(ReadOnlySpan<byte> buffer) => buffer.CopyTo(builder.Request(buffer.Length).Span);

		public override void Flush() { }
		public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;

		#region [ Boilerplate                  ]

		public override bool CanRead => false;
		public override bool CanSeek => false;
		public override bool CanTimeout => false;

		public override long Position
		{
			get => throw new NotSupportedException();
			set => throw new NotSupportedException();
		}

		public override void CopyTo(Stream destination, int bufferSize) => throw new NotSupportedException();
		public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) => throw new NotSupportedException();

		public override int ReadByte() => throw new NotSupportedException();
		public override int Read(Span<byte> buffer) => throw new NotSupportedException();
		public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
		public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state) => throw new NotSupportedException();
		public override int EndRead(IAsyncResult asyncResult) => throw new NotSupportedException();
		public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => throw new NotSupportedException();
		public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => throw new NotSupportedException();

		public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state) => throw new NotSupportedException();
		public override void EndWrite(IAsyncResult asyncResult) => throw new NotSupportedException();
		public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => throw new NotSupportedException();
		public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) => throw new NotSupportedException();

		public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
		public override void SetLength(long value) => throw new NotSupportedException();

		public override void Close() { }
		protected override void Dispose(bool disposing) { }
		public override ValueTask DisposeAsync() => new ValueTask();

		#endregion
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
