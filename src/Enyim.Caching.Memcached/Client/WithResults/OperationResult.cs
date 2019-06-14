using System;
using System.IO;
using System.Threading.Tasks;

namespace Enyim.Caching.Memcached
{
	public readonly struct OperationResult
	{
		private readonly OperationStatus statusCode;
		private readonly bool isInitialized;

		public OperationResult(Exception exception)
		{
			this.statusCode = (OperationStatus)Protocol.Status.InternalError;
			Cas = 0;
			Exception = exception;
			isInitialized = true;
		}

		public OperationResult(OperationStatus statusCode, ulong cas)
		{
			this.statusCode = statusCode;
			Cas = cas;
			Exception = null;
			isInitialized = true;
		}

		public OperationStatus StatusCode => isInitialized ? statusCode : OperationStatus.InternalError;
		public ulong Cas { get; }
		public Exception? Exception { get; }

		public bool Success => StatusCode == Protocol.Status.Success;

		public void Deconstruct(out OperationStatus statusCode, out ulong cas)
		{
			statusCode = StatusCode;
			cas = Cas;
		}

		public void Deconstruct(out OperationStatus statusCode, out ulong cas, out Exception? exception)
		{
			statusCode = StatusCode;
			cas = Cas;
			exception = Exception;
		}
	}

	public enum OperationStatus
	{
		Success = 0,
		KeyNotFound = 0x0001,
		KeyExists = 0x0002,
		ValueTooLarge = 0x0003,
		InvalidArguments = 0x0004,
		ItemNotStored = 0x0005,
		IncrDecrNonNumericValue = 0x0006,
		NotMyVBucket = 0x0007,
		AuthenticationError = 0x0008,
		AuthenticationContinue = 0x0009,
		UnknownCommand = 0x0081,
		OutOfMemory = 0x0082,
		NotSupported = 0x0083,
		InternalError = 0x0084,
		Busy = 0x0085,
		TemporaryFailure = 0x0086
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
