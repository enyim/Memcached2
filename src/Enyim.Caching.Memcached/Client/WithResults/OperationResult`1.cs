using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;

namespace Enyim.Caching.Memcached
{
	public readonly struct OperationResult<T>
	{
		private readonly OperationStatus statusCode;
		private readonly bool isInitialized;

		public OperationResult(Exception exception)
		{
			Value = default;
			this.statusCode = OperationStatus.InternalError;
			Cas = 0;
			Exception = exception;
			isInitialized = true;
		}

		public OperationResult([MaybeNull]T value, OperationStatus statusCode, ulong cas)
		{
			Value = value;
			this.statusCode = statusCode;
			Cas = cas;
			Exception = null;
			isInitialized = true;
		}

		[MaybeNull][AllowNull]
		public T Value { get; }
		public OperationStatus StatusCode => isInitialized ? statusCode : OperationStatus.InternalError;
		public ulong Cas { get; }
		public Exception? Exception { get; }

		public bool Success => StatusCode == Protocol.Status.Success;

		public void Deconstruct([MaybeNull]out T value, out ulong cas)
		{
			value = Value;
			cas = Cas;
		}

		public void Deconstruct([MaybeNull]out T value, out ulong cas, out OperationStatus statusCode)
		{
			value = Value;
			cas = Cas;
			statusCode = StatusCode;
		}

		public void Deconstruct([MaybeNull]out T value, out ulong cas, out OperationStatus statusCode, out Exception? exception)
		{
			value = Value;
			cas = Cas;
			statusCode = StatusCode;
			exception = Exception;
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
