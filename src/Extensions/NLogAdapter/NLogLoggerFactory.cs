using System;

namespace Enyim.Caching.Diagnostics
{
	using NLogLogLevel = NLog.LogLevel;
	using INLogLogger = NLog.ILogger;

	public class NLogLoggerFactory : ILoggerFactory
	{
		private readonly NLog.LogFactory factory;

		public NLogLoggerFactory(NLog.LogFactory? factory = null)
		{
			this.factory = factory ?? NLog.LogManager.LogFactory;
		}

		public ILogger Create(string name) => new LoggerAdapter(factory.GetLogger(name));

		#region [ LoggerAdapter                ]

		private class LoggerAdapter : ILogger
		{
			private readonly INLogLogger instance;

			public LoggerAdapter(INLogLogger instance)
			{
				this.instance = instance;
			}

			private static readonly NLogLogLevel[] levelMap = new[]
			{
				/* LogLevel.Trace       = */ NLogLogLevel.Trace,
				/* LogLevel.Information = */ NLogLogLevel.Info,
				/* LogLevel.Warning     = */ NLogLogLevel.Warn,
				/* LogLevel.Error       = */ NLogLogLevel.Error
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
