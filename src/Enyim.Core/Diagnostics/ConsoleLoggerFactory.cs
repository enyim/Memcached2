using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Enyim.Diagnostics
{
	public class ConsoleLoggerFactory : ILoggerFactory
	{
		private readonly LogLevel minLevel;

		public ConsoleLoggerFactory(LogLevel minLevel = LogLevel.Trace)
		{
			this.minLevel = minLevel;
		}

		public ILogger Create(string name) => new Logger(name, minLevel);

		public static string FormatMessageTemplate(string message)
		{
			// just dumbly replace {tokens} with their index in the
			// message to be able use String.Format
			if (String.IsNullOrEmpty(message)) return message;

			var span = message.AsSpan();
			var retval = new StringBuilder();
			var counter = 0;
			var shouldClose = false;

			const string ValueStart = "<<";
			const string ValueEnd = ">>";

			while (!span.IsEmpty)
			{
				var c = span[0];

				if (c == '{')
				{
					var template = span.Slice(1);
					var inTemplate = false;

					while (!template.IsEmpty)
					{
						var t = template[0];
						if (!inTemplate && Char.IsDigit(t))
						{
							break;
						}
						else if (t == '}' || t == ',' || t == ':')
						{
							Debug.Assert(inTemplate);

							retval.Append(ValueStart);
							retval.Append('{');
							retval.Append(counter++);

							shouldClose = true;
							span = template;
							c = t;
							break;
						}
						else
						{
							inTemplate = true;
						}

						template = template.Slice(1);
					}
				}

				retval.Append(c);
				if (c == '}' && shouldClose)
				{
					retval.Append(ValueEnd);
					shouldClose = false;
				}
				span = span.Slice(1);
			}

			return retval.ToString();
		}

		#region [ Logger                       ]

		private class Logger : ILogger
		{
			private readonly string name;
			private readonly LogLevel minLevel;

			public Logger(string name, LogLevel minLevel = LogLevel.Trace)
			{
				this.name = name;
				this.minLevel = minLevel;
			}

			public bool IsEnabled(LogLevel level) => level >= minLevel;

			public void Log(LogLevel level, Exception? exception, string? message, params object[] args)
			{
				if (level < minLevel) return;
				if (message == null) return;

				var old = Console.ForegroundColor;
				Console.ForegroundColor = level switch
				{
					LogLevel.Trace => ConsoleColor.DarkGray,
					LogLevel.Information => ConsoleColor.White,
					LogLevel.Warning => ConsoleColor.Magenta,
					LogLevel.Error => ConsoleColor.Yellow,
					_ => throw new ArithmeticException(nameof(level))
				};

				Console.Write(DateTime.Now.ToString("HH:mm:ss.fffff", CultureInfo.InvariantCulture));

				Console.Write(level switch
				{
					LogLevel.Trace => " [TRACE] ",
					LogLevel.Information => " [INFO ] ",
					LogLevel.Warning => " [WARN ] ",
					LogLevel.Error => " [ERROR] ",
					_ => throw new ArithmeticException(nameof(level))
				});

				Console.Write(name);
				Console.Write(" - ");

				if (exception != null) Console.WriteLine(exception);
				Console.WriteLine(FormatMessageTemplate(message), args);

				Console.ForegroundColor = old;
			}
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
