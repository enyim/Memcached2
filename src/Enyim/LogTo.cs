using System;
using System.Collections.Generic;
using System.Linq;

namespace Enyim
{
	/// <summary>
	/// Helper for writing logging code. Calls to this calls will be rewritten to guarded ILog invokations by the LogTo rewriter.
	/// </summary>
	[Internal.MapLogTo(ILog = typeof(ILogger))]
	public static class LogTo
	{
		public static void Trace(string message, Exception? exception = null, params object[] args) { }
		public static void Information(string message, Exception? exception = null, params object[] args) { }
		public static void Warning(string message, Exception? exception = null, params object[] args) { }
		public static void Error(string message, Exception? exception = null, params object[] args) { }
	}

	namespace Internal
	{
#pragma warning disable CS8618 // Non-nullable field is uninitialized.

		// Support attribute for the LogTo rewriter
		[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
		internal class MapLogToAttribute : System.Attribute
		{
			public Type ILog { get; set; }
		}

		// Support attribute for the LogTo rewriter
		[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
		internal class FactoryAttribute : System.Attribute
		{
			public Type Type { get; set; }
			public string MethodName { get; set; } = "Create";
		}

#pragma warning restore CS8618 // Non-nullable field is uninitialized.
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
