using System;
using System.Collections.Generic;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Xunit;
using Moq;

namespace Enyim.Caching.Tests
{
	public class SerilogLoggerAdapterTests
	{
		[Theory]
		[MemberData(nameof(DataForTest))]
		public void Test(Serilog.Events.LogEventLevel serilogLevel, Enyim.LogLevel enyimLevel, string message, Exception exception)
		{
			var mock = new Moq.Mock<ILogEventSink>();
			var root = new LoggerConfiguration()
							.MinimumLevel.Verbose()
							.WriteTo.Sink(mock.Object)
							.CreateLogger();

			Enyim.LogManager.AssignFactory(new Enyim.Caching.Diagnostics.SerilogLoggerFactory(root));
			LogManager.Create(typeof(SerilogLoggerAdapterTests)).Log(enyimLevel, exception, message);

			mock.Verify(sink => sink.Emit(It.Is<LogEvent>(e => e.Level == serilogLevel && e.MessageTemplate.Text == message && e.Exception == exception)));
		}

		public static IEnumerable<object[]> DataForTest
		{
			get
			{
				return new[]
				{
					new object[] { LogEventLevel.Verbose , LogLevel.Trace, "this is a Verbose message with exception", new ArgumentException() },
					new object[] { LogEventLevel.Information, LogLevel.Information, "this is an Information message with exception", new ArgumentException() },
					new object[] { LogEventLevel.Warning, LogLevel.Warning, "this is a Warning message with exception", new ArgumentException() },
					new object[] { LogEventLevel.Error, LogLevel.Error, "this is an Error message with exception", new ArgumentException() },

					new object[] { LogEventLevel.Verbose, LogLevel.Trace, "this is a Verbose message", new ArgumentException() },
					new object[] { LogEventLevel.Information, LogLevel.Information, "this is an Information message", new ArgumentException() },
					new object[] { LogEventLevel.Warning, LogLevel.Warning, "this is a Warning message", new ArgumentException() },
					new object[] { LogEventLevel.Error, LogLevel.Error, "this is an Error message", new ArgumentException() }
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
