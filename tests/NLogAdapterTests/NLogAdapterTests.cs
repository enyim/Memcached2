using System;
using System.Collections.Generic;
using NLog;
using Xunit;
using Moq;
using Moq.Protected;

namespace Enyim.Caching.Tests
{
	using NLogLogLevel = NLog.LogLevel;

	public class NLogAdapterTests
	{
		[Theory]
		[MemberData(nameof(DataForTest))]
		public void Test(NLogLogLevel logLevel, Enyim.LogLevel enyimLevel, string message, Exception exception)
		{
			var mock = new Moq.Mock<NLog.Targets.TargetWithContext>(MockBehavior.Loose) { CallBase = true };

			var config = new NLog.Config.LoggingConfiguration();
			config.AddRuleForAllLevels(mock.Object);

			LogManager.AssignFactory(new Enyim.Caching.Diagnostics.NLogLoggerFactory(new NLog.LogFactory(config)));
			LogManager.Create(typeof(NLogAdapterTests)).Log(enyimLevel, exception, message);

			mock.Protected().Verify("Write", Times.Once(),
										ItExpr.Is<LogEventInfo>(info => info.Level == logLevel
																&& info.Message == message
																&& info.Exception == exception));
		}

		public static IEnumerable<object[]> DataForTest
		{
			get
			{
				return new[]
				{
					new object[] { NLogLogLevel.Trace, LogLevel.Trace, "this is a Verbose message with exception", new ArgumentException() },
					new object[] { NLogLogLevel.Info, LogLevel.Information, "this is an Information message with exception", new ArgumentException() },
					new object[] { NLogLogLevel.Warn, LogLevel.Warning, "this is a Warning message with exception", new ArgumentException() },
					new object[] { NLogLogLevel.Error, LogLevel.Error, "this is an Error message with exception", new ArgumentException() },

					new object[] { NLogLogLevel.Trace, LogLevel.Trace, "this is a Verbose message", new ArgumentException() },
					new object[] { NLogLogLevel.Info, LogLevel.Information, "this is an Information message", new ArgumentException() },
					new object[] { NLogLogLevel.Warn, LogLevel.Warning, "this is a Warning message", new ArgumentException() },
					new object[] { NLogLogLevel.Error, LogLevel.Error, "this is an Error message", new ArgumentException() }
				};
			}
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
