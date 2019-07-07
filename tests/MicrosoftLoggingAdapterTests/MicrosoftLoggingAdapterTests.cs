using System;
using System.Collections.Generic;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;

namespace Enyim.Caching.Tests
{
	using MicrosoftLogLevel = Microsoft.Extensions.Logging.LogLevel;
	using IMicrosoftLogger = Microsoft.Extensions.Logging.ILogger;
	using IMicrosoftLoggerProvider = Microsoft.Extensions.Logging.ILoggerProvider;

	public class MicrosoftLoggingAdapterTests
	{
		[Theory]
		[MemberData(nameof(DataForTest))]
		public void Test(MicrosoftLogLevel msLogLevel, Enyim.LogLevel enyimLevel, string message, Exception exception)
		{
			var mockLogger = new Moq.Mock<IMicrosoftLogger>();
			var mockProvider = new Moq.Mock<IMicrosoftLoggerProvider>();

			mockProvider.Setup(p => p.CreateLogger(It.IsAny<string>())).Returns(mockLogger.Object);

			LogManager.AssignFactory(new Enyim.Diagnostics.MicrosoftLoggerFactory(new LoggerFactory(new[] { mockProvider.Object })));
			LogManager.Create(typeof(MicrosoftLoggingAdapterTests)).Log(enyimLevel, exception, message);

			// TODO capture state & formatter and validate the message
			mockLogger.Verify(sink => sink.Log(
											It.Is<MicrosoftLogLevel>(v => v == msLogLevel),
											It.IsAny<EventId>(),
											It.IsAny<object>(),
											It.Is<Exception>(v => v == exception),
											It.IsAny<Func<object, Exception, string>>()));
		}

		public static IEnumerable<object[]> DataForTest
		{
			get
			{
				return new[]
				{
					new object[] { MicrosoftLogLevel.Trace, LogLevel.Trace, "this is a Verbose message with exception", new ArgumentException() },
					new object[] { MicrosoftLogLevel.Information, LogLevel.Information, "this is an Information message with exception", new ArgumentException() },
					new object[] { MicrosoftLogLevel.Warning, LogLevel.Warning, "this is a Warning message with exception", new ArgumentException() },
					new object[] { MicrosoftLogLevel.Error, LogLevel.Error, "this is an Error message with exception", new ArgumentException() },

					new object[] { MicrosoftLogLevel.Trace, LogLevel.Trace, "this is a Verbose message", new ArgumentException() },
					new object[] { MicrosoftLogLevel.Information, LogLevel.Information, "this is an Information message", new ArgumentException() },
					new object[] { MicrosoftLogLevel.Warning, LogLevel.Warning, "this is a Warning message", new ArgumentException() },
					new object[] { MicrosoftLogLevel.Error, LogLevel.Error, "this is an Error message", new ArgumentException() }
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
