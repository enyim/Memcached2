using System;

namespace Enyim.Caching.Diagnostics
{
	using SerilogLogLevel = Serilog.Events.LogEventLevel;
	using ISerilogLogger = Serilog.ILogger;

	public class SerilogLoggerFactory : ILoggerFactory
	{
		private readonly ISerilogLogger rootLogger;

		public SerilogLoggerFactory(ISerilogLogger? logger = null)
		{
			rootLogger = logger ?? Serilog.Log.Logger;
		}

		public ILogger Create(string name) => new LoggerAdapter(rootLogger.ForContext(Serilog.Core.Constants.SourceContextPropertyName, name));

		#region [ LoggerAdapter                ]

		private class LoggerAdapter : ILogger
		{
			private readonly ISerilogLogger instance;

			public LoggerAdapter(ISerilogLogger instance)
			{
				this.instance = instance;
			}

			private static readonly SerilogLogLevel[] levelMap = new[]
			{
				/* LogLevel.Trace       = */ SerilogLogLevel.Verbose,
				/* LogLevel.Information = */ SerilogLogLevel.Information,
				/* LogLevel.Warning     = */ SerilogLogLevel.Warning,
				/* LogLevel.Error       = */ SerilogLogLevel.Error
			};

			public bool IsEnabled(LogLevel level) => instance.IsEnabled(levelMap[(int)level]);
			public void Log(LogLevel level, Exception? exception, string? message, params object[] args) => instance.Write(levelMap[(int)level], exception, message, args);
		}

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
