#if DIAGNOSTICS
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Enyim
{
	public static class Metrics
	{
		private static readonly object CacheLock = new Object();

		private static readonly Dictionary<string, IMetric> Cache = new Dictionary<string, IMetric>();
		private static IMetric[] allCounters;

		private static IMetricFactory Factory = new Internal.DefaultMetricFactory();

		public static void AssignFactory(IMetricFactory factory) => Factory = factory;

		public static ReadOnlySpan<IMetric> GetAll()
		{
			if (allCounters == null)
			{
				lock (CacheLock)
				{
					if (allCounters == null)
					{
						allCounters = Cache.OrderBy(kvp => kvp.Key).Select(kvp => kvp.Value).ToArray();
					}
				}
			}

			return allCounters.AsSpan();
		}

		private static T New<T>(string name, string instance, Func<T> create) where T : class, IMetric
		{
			T retval;
			var key = String.IsNullOrEmpty(instance)
						? name
						: name + "\t" + instance;

			lock (CacheLock)
			{
				if (Cache.TryGetValue(key, out var tmp))
				{
					retval = tmp as T ?? throw new InvalidOperationException($"Expected metric {name} to be {typeof(T)} but it's {tmp.GetType()}");
				}
				else
				{
					retval = create();
					Cache.Add(key, retval);
					allCounters = null;
				}
			}

			return retval;
		}

		public static ICounter Counter(string name, string instance)
		{
			if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
			if (String.IsNullOrEmpty(instance)) throw new ArgumentNullException(nameof(instance));

			var parent = New(name, null, () => Factory.Counter(name));

			return New(name, instance, () => Factory.Counter(parent, instance));
		}

		public static IMeter Meter(string name, string instance, Interval interval)
		{
			if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
			if (String.IsNullOrEmpty(instance)) throw new ArgumentNullException(nameof(instance));

			var parent = New(name, null, () => Factory.Meter(name, interval));

			return New(name, instance, () => Factory.Meter(parent, instance, interval));
		}

		public static IGauge Gauge(string name, string instance)
		{
			if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
			if (String.IsNullOrEmpty(instance)) throw new ArgumentNullException(nameof(instance));

			var parent = New(name, null, () => Factory.Gauge(name));

			return New(name, instance, () => Factory.Gauge(parent, instance));
		}
	}

	public interface IMetricFactory
	{
		ICounter Counter(string name);
		IMeter Meter(string name, Interval interval);
		IGauge Gauge(string name);

		ICounter Counter(ICounter parent, string instance);
		IMeter Meter(IMeter parent, string instance, Interval interval);
		IGauge Gauge(IGauge parent, string instance);
	}

	public interface IMetric
	{
		IMetric Parent { get; }
		string Name { get; }
		string Instance { get; }
	}

	public interface IGauge : IMetric
	{
		long Value { get; }
		long Min { get; }
		long Max { get; }

		void Set(long value);
	}

	public interface ICounter : IMetric
	{
		long Count { get; }

		void Reset();
		void IncrementBy(int value);
	}

	public interface IMeter : ICounter
	{
		double Rate { get; }
		Interval Interval { get; }
	}

	public enum Interval
	{
		Nanoseconds,
		Microseconds,
		Milliseconds,
		Seconds,
		Minutes,
		Hours,
		Days
	}

	public static class MetricExtensions
	{
		public static void Increment(this ICounter counter) => counter.IncrementBy(1);
		public static void Decrement(this ICounter counter) => counter.IncrementBy(-1);
		public static void DecrementBy(this ICounter counter, int value) => counter.IncrementBy(-value);
	}

	#region [ MetricsVisitor               ]

	internal abstract class MetricsVisitor
	{
		public virtual void Visit(ReadOnlySpan<IMetric> metrics)
		{
			foreach (var m in metrics)
				Visit(m);
		}

		protected virtual void Visit(IMetric metric)
		{
			switch (metric)
			{
				case IMeter meter: Visit(meter); break;
				case ICounter counter: Visit(counter); break;
				case IGauge gauge: Visit(gauge); break;
			}
		}

		protected abstract void Visit(IMeter meter);
		protected abstract void Visit(ICounter counter);
		protected abstract void Visit(IGauge gauge);
	}

	#endregion
	#region [ ConsoleReporter              ]

	public class ConsoleReporter
	{
		private readonly StringBuilder builder;
		private readonly StringBuilderVisitor visitor;

		public ConsoleReporter()
		{
			builder = new StringBuilder();
			visitor = new StringBuilderVisitor(builder);
		}

		public void Report()
		{
			builder.Length = 0;
			visitor.Visit(Metrics.GetAll());

			Console.Write(builder.ToString());
		}

		public void StartPeriodicReporting(int time, Interval interval, CancellationToken cancellationToken)
		{
			var delay = (int)IntervalConverter.Convert(time, from: interval, to: Interval.Milliseconds);

			Task.Factory.StartNew(async () =>
			{
				try
				{
					while (!cancellationToken.IsCancellationRequested)
					{
						Report();

						await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
					}
				}
				catch { }
			}, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);
		}

		#region [ StringBuilderVisitor         ]

		internal class StringBuilderVisitor : MetricsVisitor
		{
			private readonly StringBuilder builder;

			public StringBuilderVisitor(StringBuilder builder)
			{
				this.builder = builder;
			}

			public override void Visit(ReadOnlySpan<IMetric> metrics)
			{
				while (!metrics.IsEmpty)
				{
					var m = metrics[0];

					if (m.Parent == null)
					{
						builder.AppendLine();
						builder.AppendFormat("{0,-22}", m.Name);
					}
					else
					{
						builder.AppendFormat("{0,20}: ", m.Instance);
					}

					Visit(m);
					builder.AppendLine("      ");

					if (m.Parent == null)
						builder.AppendLine("========================================");

					metrics = metrics.Slice(0);
				}
			}

			protected override void Visit(IMeter meter) => builder.AppendFormat("{0:0.##}/{1} ({2})", meter.Rate, meter.Interval.ToString().ToLower(), meter.Count);
			protected override void Visit(ICounter counter) => builder.Append(counter.Count);
			protected override void Visit(IGauge gauge) => builder.AppendFormat("{0} ({1}/{2})", gauge.Value, gauge.Min, gauge.Max);
		}

		#endregion
	}

	#endregion
	#region [ IntervalConverter            ]

	internal static class IntervalConverter
	{
		private static readonly long[][] ConversionMatrix = new long[][]
		{
				/* Nanoseconds  */ null,
				/* Microseconds */ new [] { 1000L },
				/* Milliseconds */ new [] { 1000_000L, 1000L },
				/* Seconds      */ new [] { 1000_000_000L, 1_000_000L, 1000L },
				/* Minutes      */ new [] { 60_000_000_000L, 60_000_000L, 60_000L, 60L },
				/* Hours        */ new [] { 3_600_000_000_000L, 3_600_000_000L, 3_600_000L, 3_600L, 60L },
				/* Days         */ new [] { 86_400_000_000_000L, 86_400_000_000L, 86_400_000L, 86_400L, 1_440L, 24L }
		};

		public static long Convert(long value, Interval from, Interval to)
		{
			return from == to
					? value
					: from > to
						? (value * ConversionMatrix[(int)from][(int)to])
						: (value / ConversionMatrix[(int)to][(int)from]);
		}
	}

	#endregion
	#region [ Internal impl.               ]

	namespace Internal
	{
		internal class DefaultMetricFactory : IMetricFactory
		{
			public ICounter Counter(string name) => new Counter(name);
			public ICounter Counter(ICounter parent, string instance) => new Counter(parent, instance);

			public IMeter Meter(string name, Interval interval) => new DefaultMeter(name, interval);
			public IMeter Meter(IMeter parent, string instance, Interval interval) => new DefaultMeter(parent, instance, interval);

			public IGauge Gauge(string name) => new DefaultGauge(name);
			public IGauge Gauge(IGauge parent, string instance) => new DefaultGauge(parent, instance);
		}

		internal abstract class DefaultMetric : IMetric
		{
			private readonly string name;

			protected DefaultMetric(string name)
			{
				this.name = name;
			}

			protected DefaultMetric(IMetric parent, string instance)
			{
				Parent = parent;
				Instance = instance;
			}

			public IMetric Parent { get; }
			public string Name => name ?? Parent.Name;
			public string Instance { get; }
		}

		internal class DefaultGauge : DefaultMetric, IGauge
		{
			public DefaultGauge(string name) : base(name) { }
			public DefaultGauge(IMetric parent, string instance) : base(parent, instance) { }

			public void Set(long value)
			{
				Value = value;

				if (Min > value) Min = value;
				if (Max < value) Max = value;

				((IGauge)Parent)?.Set(value);
			}

			public long Value { get; private set; }
			public long Min { get; private set; }
			public long Max { get; private set; }
		}

		internal class Counter : DefaultMetric, ICounter
		{
			// TODO tune this
			private const int STRIPE_COUNT = 32;

			// we have two counters and we pad them with 64 bytes (length of the cache-line)
			private const int STRIPE_LENGTH = 10;

			private const int IDX_THREAD_ID = 0;
			private const int IDX_VALUE = 1;

			private long global;
			private long[] data;

			public Counter(string name)
				: base(name)
			{
				Initialize();
			}

			public Counter(IMetric parent, string instance)
				: base(parent, instance)
			{
				Initialize();
			}

			public long Count
			{
				get
				{
					var retval = global;
					var d = data;

					for (var i = STRIPE_LENGTH; i < d.Length; i += STRIPE_LENGTH)
					{
						retval += d[i + IDX_VALUE];
					}

					return retval;
				}
			}

			private void Initialize()
			{
				// add an extra padding to the beginning of the array to avoid false-sharing with the array's length
				Volatile.Write(ref data, new long[(STRIPE_LENGTH * STRIPE_COUNT) + STRIPE_LENGTH]);
				Volatile.Write(ref global, 0);
			}

			public virtual void Reset()
			{
				Initialize();

				((ICounter)Parent)?.Reset();
			}

			public void IncrementBy(int value) => ChangeBy(value);
			public void DecrementBy(int value) => ChangeBy(-value);

			private void ChangeBy(int by)
			{
				var p = Parent as Counter;
				p?.ChangeBy(by);

				var threadId = Thread.CurrentThread.ManagedThreadId;
				var hash = threadId;

				for (var i = 0; i < 3; i++)
				{
					var index = ((hash % STRIPE_COUNT) + 1) * STRIPE_LENGTH;
					var bucketThread = data[index + IDX_THREAD_ID];

					if (bucketThread == threadId)
					{
						data[index + IDX_VALUE] += by;
						return;
					}
					else if (bucketThread == 0)
					{
						if (Interlocked.CompareExchange(ref data[index + IDX_VALUE], threadId, 0) == 0)
						{
							data[index + IDX_VALUE] = by;
							return;
						}
					}

					hash ^= (hash << 5) + (hash >> 2) + threadId;
				}

				Interlocked.Add(ref global, by);
			}
		}

		internal class DefaultMeter : Counter, IMeter
		{
			private static readonly long NanoTick = 1000 * 1000 * 1000 / Stopwatch.Frequency;

			private readonly Stopwatch stopwatch;
			private readonly Interval interval;

			public DefaultMeter(string name, Interval interval)
				: base(name)
			{
				this.interval = interval;
				stopwatch = Stopwatch.StartNew();
			}

			public DefaultMeter(IMetric parent, string instance, Interval interval)
				: base(parent, instance)
			{
				this.interval = interval;
				stopwatch = Stopwatch.StartNew();
			}

			public Interval Interval => interval;

			public override void Reset()
			{
				base.Reset();
				stopwatch.Restart();
			}

			public double Rate
			{
				get
				{
					var by = IntervalConverter.Convert(stopwatch.ElapsedTicks * NanoTick, Interval.Nanoseconds, interval);

					// handle the case when we're read just after the Meter was reset
					return by == 0
							? Count
							: (double)Count / by;
				}
			}
		}
	}

	#endregion
}

#endif

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
