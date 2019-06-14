using System;
using System.Collections.Generic;
using System.Linq;
using Enyim.Caching.Memcached;
using Xunit;

namespace Enyim.Caching.Memcached.Client
{
	public class OperationResultTests
	{
		[Fact]
		public void OperationResult_Can_Be_Deconstructed_As_StatusCode_And_Cas()
		{
			const OperationStatus ExpectedStatus = OperationStatus.IncrDecrNonNumericValue;
			const ulong ExpectedCas = 1234;

			var (status, cas) = new OperationResult(ExpectedStatus, ExpectedCas);

			Assert.Equal(ExpectedStatus, status);
			Assert.Equal(ExpectedCas, cas);
		}

		[Fact]
		public void OperationResult_T_Can_Be_Deconstructed_As_Result_And_Cas()
		{
			const short ExpectedValue = 256;
			const ulong ExpectedCas = 1234;

			var (value, cas) = new OperationResult<short>(ExpectedValue, OperationStatus.AuthenticationContinue, ExpectedCas);

			Assert.Equal(ExpectedValue, value);
			Assert.Equal(ExpectedCas, cas);
		}

		[Fact]
		public void OperationResult_T_Can_Be_Deconstructed_As_Result_And_StatusCode_And_Cas()
		{
			const short ExpectedValue = 256;
			const ulong ExpectedCas = 1234;
			const OperationStatus ExpectedStatus = OperationStatus.IncrDecrNonNumericValue;

			var (value, cas, status) = new OperationResult<short>(ExpectedValue, ExpectedStatus, ExpectedCas);

			Assert.Equal(ExpectedValue, value);
			Assert.Equal(ExpectedCas, cas);
			Assert.Equal(ExpectedStatus, status);
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
