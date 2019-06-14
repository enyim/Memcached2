using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Enyim.Caching.Memcached
{
	public class MemcachedServer : IDisposable
	{
		private static readonly string BasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools");
		private static readonly string ExePath = Path.Combine(BasePath, "memcached.exe");

		private static int PortCounter = 11200;

		private readonly bool verbose;
		private readonly int maxMem;
		private readonly bool hidden;

		private Process process;

		public MemcachedServer(int port, bool verbose = false, int maxMem = 32, bool hidden = false)
		{
			this.Port = port;
			this.verbose = verbose;
			this.maxMem = maxMem;
			this.hidden = hidden;
		}

		public int Port { get; }
		public bool DidStart => process != null;

		public static MemcachedServer WithAutoPort(bool verbose = false, int maxMem = 32, bool hidden = false)
		{
			return new MemcachedServer(Interlocked.Increment(ref PortCounter), verbose, maxMem, hidden);
		}

		public void Start()
		{
			if (DidStart) throw new InvalidOperationException("Already started");

			var args = $"-B binary -p {Port} -m {maxMem} -E default_engine.so";
			if (verbose) args = args.Insert(0, "-v ");

			process = Process.Start(new ProcessStartInfo
			{
				Arguments = args,
				FileName = ExePath,
				WorkingDirectory = BasePath,
				CreateNoWindow = false,
				UseShellExecute = true,
				WindowStyle = hidden ? ProcessWindowStyle.Hidden : ProcessWindowStyle.Normal
			});

			AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
			AppDomain.CurrentDomain.DomainUnload += CurrentDomain_ProcessExit;
		}

		private void CurrentDomain_ProcessExit(object sender, EventArgs e) => Dispose();

		public void Dispose()
		{
			if (process != null)
			{
				try
				{
					using (process)
					{
						process.Kill();
					}
				}
				catch { }

				process = null;
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
