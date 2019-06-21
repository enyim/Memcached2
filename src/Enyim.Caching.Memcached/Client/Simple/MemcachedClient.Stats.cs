using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Enyim.Caching.Memcached
{
	public partial class MemcachedClient
	{
		public async Task<MemcachedStats> StatsAsync(string? type)
		{
			var ops = new List<(INode node, Operations.StatsOperation op)>();
			var tasks = cluster.Broadcast((node, state) =>
			{
				var retval = new Operations.StatsOperation(state.pool, state.type);
				state.ops.Add((node, retval));

				return retval;
			}, (pool, type, ops));

			var successful = (await WhenAllUnfailed(tasks).ConfigureAwait(false)).ToHashSet();
			var retval = new MemcachedStats();

#pragma warning disable CS8619
			foreach (var (node, op) in ops)
			{
				if (successful.Contains(op))
					retval.Set(node.EndPoint, op.Stats);
			}
#pragma warning enable CS8619

			return retval;
		}

		protected async Task<T[]> WhenAllUnfailed<T>(Task<T>[] tasks)
		{
			try
			{
				await Task.WhenAll(tasks).ConfigureAwait(false);
			}
			catch (IOException) { }

			var j = 0;
			var retval = new T[tasks.Length];

			for (var i = 0; i < tasks.Length; i++)
			{
				var t = tasks[i];
				if (t.Status == TaskStatus.RanToCompletion)
					retval[j++] = t.Result; // this is safe, as the task is already completed
			}

			if (j != retval.Length)
				Array.Resize(ref retval, j);

			return retval;
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
