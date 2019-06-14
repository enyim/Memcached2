using System;
using Microsoft.Extensions.Logging;

namespace Enyim.Caching.Diagnostics
{
	using MicrosoftLogLevel = Microsoft.Extensions.Logging.LogLevel;
	using IMicrosoftLoggerFactory = Microsoft.Extensions.Logging.ILoggerFactory;
	using IMicrosoftLogger = Microsoft.Extensions.Logging.ILogger;

	public class MicrosoftLoggerFactory : ILoggerFactory
	{
		private readonly IMicrosoftLoggerFactory loggerFactory;

		public MicrosoftLoggerFactory(IMicrosoftLoggerFactory loggerFactory)
		{
			this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
		}

		public ILogger Create(string name) => new LoggerAdapter(loggerFactory.CreateLogger(name));

		#region [ LoggerAdapter                ]

		private class LoggerAdapter : ILogger
		{
			private readonly IMicrosoftLogger instance;

			public LoggerAdapter(IMicrosoftLogger instance)
			{
				this.instance = instance;
			}

			private static readonly MicrosoftLogLevel[] levelMap = new[]
			{
				/* LogLevel.Trace       = */ MicrosoftLogLevel.Trace,
				/* LogLevel.Information = */ MicrosoftLogLevel.Information,
				/* LogLevel.Warning     = */ MicrosoftLogLevel.Warning,
				/* LogLevel.Error       = */ MicrosoftLogLevel.Error
			};

			public bool IsEnabled(LogLevel level) => instance.IsEnabled(levelMap[(int)level]);
			public void Log(LogLevel level, Exception? exception, string? message, params object[] args) => instance.Log(levelMap[(int)level], exception, message, args);
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
