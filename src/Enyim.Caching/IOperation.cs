using System;

namespace Enyim.Caching
{
	public interface IOperation
	{
		/// <summary>
		/// Builds a request object.
		/// </summary>
		/// <returns>a request object which can be sent on the wire</returns>
		IRequest CreateRequest();

		/// <summary>
		/// Determines whether the request read from the wire is handled by this operation instance.
		/// </summary>
		/// <param name="response"></param>
		/// <returns>true if the response is matching the request of this operation</returns>
		bool Handles(IResponse response);

		/// <summary>
		/// Processes the response read from the wire.
		/// </summary>
		/// <param name="response"></param>
		/// <returns>returns true if more responses must be retrieved to complete this operaton (i.e. IO is pending); false otherwise</returns>
		bool ProcessResponse(IResponse? response);

		void Failed(Exception e);
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
