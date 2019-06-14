using System;
using System.Diagnostics;

namespace Enyim.Caching.Memcached.Operations
{
	internal abstract class MemcachedOperationBase : IOperation
	{
		protected MemcachedOperationBase() { }

		protected abstract IMemcachedRequest CreateRequest();

		public uint CorrelationId { get; private set; }
		public int StatusCode { get; protected set; }
		public ulong Cas { get; set; }

		IRequest IOperation.CreateRequest()
		{
			var retval = CreateRequest();
			CorrelationId = retval.CorrelationId;

			return retval;
		}

		bool IOperation.Handles(IResponse response)
		{
			return CorrelationId == ((BinaryResponse)response).CorrelationId;
		}

		void IOperation.Failed(Exception e)
		{
			Failed(e);
		}

		// NodeBase calls ProcessResponse with incoming responses until it returns false
		// this allows handling multi-response commands are handled (like STATS)
		bool IOperation.ProcessResponse(IResponse? response)
		{
			var binary = response as BinaryResponse;

			if (binary == null)
			{
				StatusCode = -1; // sentinel value to make sure all operations handle the null case
				Cas = 0; // user may have provided a CAS value, so reset it (silent ops do not send CAS back)
			}
			else
			{
				StatusCode = binary.StatusCode;
				Cas = binary.CAS;
			}

			var retval = ParseResult(binary);
			Debug.Assert(StatusCode >= 0, "StatusCode was not set by ParseResult");

			return retval;
		}

		protected abstract bool ParseResult(BinaryResponse? response);
		protected virtual void Failed(Exception e)
		{
			StatusCode = e.HResult;
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
