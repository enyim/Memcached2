using System;

namespace Enyim
{
	public static class LoggerExtensions
	{
		public static void Trace(this ILogger logger, string message) => logger?.Log(LogLevel.Trace, null, message);
		public static void Trace(this ILogger logger, string message, params object[] args) => logger?.Log(LogLevel.Trace, null, message, args);
		public static void Trace(this ILogger logger, Exception exception) => logger?.Log(LogLevel.Trace, exception, null);
		public static void Trace(this ILogger logger, Exception exception, string message, params object[] args) => logger?.Log(LogLevel.Trace, exception, message, args);

		public static void Information(this ILogger logger, string message) => logger?.Log(LogLevel.Information, null, message);
		public static void Information(this ILogger logger, string message, params object[] args) => logger?.Log(LogLevel.Information, null, message, args);
		public static void Information(this ILogger logger, Exception exception) => logger?.Log(LogLevel.Information, exception, null);
		public static void Information(this ILogger logger, Exception exception, string message, params object[] args) => logger?.Log(LogLevel.Information, exception, message, args);

		public static void Warning(this ILogger logger, string message) => logger?.Log(LogLevel.Warning, null, message);
		public static void Warning(this ILogger logger, string message, params object[] args) => logger?.Log(LogLevel.Warning, null, message, args);
		public static void Warning(this ILogger logger, Exception exception) => logger?.Log(LogLevel.Warning, exception, null);
		public static void Warning(this ILogger logger, Exception exception, string message, params object[] args) => logger?.Log(LogLevel.Warning, exception, message, args);

		public static void Error(this ILogger logger, string message) => logger?.Log(LogLevel.Error, null, message);
		public static void Error(this ILogger logger, string message, params object[] args) => logger?.Log(LogLevel.Error, null, message, args);
		public static void Error(this ILogger logger, Exception exception) => logger?.Log(LogLevel.Error, exception, null);
		public static void Error(this ILogger logger, Exception exception, string message, params object[] args) => logger?.Log(LogLevel.Error, exception, message, args);
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
