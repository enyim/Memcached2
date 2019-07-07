using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;

using Enyim.Caching;
using Enyim.Caching.Memcached;
using Enyim.Caching.Memcached.Internal;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
	public static class MemcachedConfigurationBuilderExtensions
	{
		public static IMemcachedConfigurationBuilder AddMemcached(this IServiceCollection services, params string[] endpoints)
		{
			return services.AddMemcached(EndPointHelper.ParseList(endpoints ?? throw new ArgumentNullException(nameof(endpoints))));
		}

		public static IMemcachedConfigurationBuilder AddMemcached(this IServiceCollection services, IEnumerable<IPEndPoint> endpoints)
		{
			var finalEndpoints = (endpoints ?? throw new ArgumentNullException(nameof(endpoints))).ToArray();
			if (finalEndpoints.Length < 1) throw new ArgumentException("Must provide at least one endpoint to connect to", nameof(endpoints));

			services.TryAddSingleton(MemoryPool<byte>.Shared);

			services.AddSingleton<IMemcachedCluster>(serviceProvider =>
			{
				var clusterOptions = serviceProvider.GetService<IMemcachedClusterOptions>() ?? new SelfResolvingClusterOptions(serviceProvider, new MemcachedClusterOptions());
				var clientOpts = serviceProvider.GetService<IMemcachedClientOptions>() ?? new SelfResolvingClientOptions(serviceProvider, new MemcachedClientOptions());

				return new MemcachedCluster(finalEndpoints, clusterOptions, clientOpts);
			});

			services.AddSingleton<IStartupFilter, ClusterStartingStartupFilter>();
			services.AddSingleton(serviceProvider => serviceProvider.GetRequiredService<IMemcachedCluster>().GetClient());

			return new TheBuilder(services);
		}

		public static IMemcachedConfigurationBuilder SetOptions(this IMemcachedConfigurationBuilder builder, IMemcachedClusterOptions options)
		{
			builder.Services.AddSingleton(options);

			return builder;
		}

		public static IMemcachedConfigurationBuilder SetOptions(this IMemcachedConfigurationBuilder builder, IMemcachedClientOptions options)
		{
			builder.Services.AddSingleton(options);

			return builder;
		}

		public static IMemcachedConfigurationBuilder SetOptions(this IMemcachedConfigurationBuilder builder, ISocketOptions options)
		{
			builder.Services.AddSingleton(options);
			builder.Services.AddTransient<ISocketFactory, AsyncSocketFactory>();

			return builder;
		}

		public static IMemcachedConfigurationBuilder UseNodeLocator<TService>(this IMemcachedConfigurationBuilder builder)
			where TService : class, INodeLocator
		{
			builder.Services.AddTransient<INodeLocator, TService>();

			return builder;
		}

		public static IMemcachedConfigurationBuilder UseNodeLocator(this IMemcachedConfigurationBuilder builder, Func<IServiceProvider, INodeLocator> implementationFactory)
		{
			builder.Services.AddTransient(implementationFactory);

			return builder;
		}

		public static IMemcachedConfigurationBuilder UseFailurePolicyFactory<TService>(this IMemcachedConfigurationBuilder builder)
			where TService : class, IFailurePolicyFactory
		{
			builder.Services.AddTransient<IFailurePolicyFactory, TService>();

			return builder;
		}

		public static IMemcachedConfigurationBuilder UseFailurePolicyFactory(this IMemcachedConfigurationBuilder builder, Func<IServiceProvider, IFailurePolicyFactory> implementationFactory)
		{
			builder.Services.AddTransient(implementationFactory);

			return builder;
		}

		public static IMemcachedConfigurationBuilder UseReconnectPolicyFactory<TService>(this IMemcachedConfigurationBuilder builder)
			where TService : class, IReconnectPolicyFactory
		{
			builder.Services.AddTransient<IReconnectPolicyFactory, TService>();

			return builder;
		}

		public static IMemcachedConfigurationBuilder UseReconnectPolicyFactory(this IMemcachedConfigurationBuilder builder, Func<IServiceProvider, IReconnectPolicyFactory> implementationFactory)
		{
			builder.Services.AddTransient(implementationFactory);

			return builder;
		}

		public static IMemcachedConfigurationBuilder UseSocketFactory<TService>(this IMemcachedConfigurationBuilder builder)
			where TService : class, ISocketFactory
		{
			builder.Services.AddTransient<ISocketFactory, TService>();

			return builder;
		}

		public static IMemcachedConfigurationBuilder UseSocketFactory(this IMemcachedConfigurationBuilder builder, Func<IServiceProvider, ISocketFactory> implementationFactory)
		{
			builder.Services.AddTransient(implementationFactory);

			return builder;
		}

		public static IMemcachedConfigurationBuilder UseKeyFormatter<TService>(this IMemcachedConfigurationBuilder builder)
			where TService : class, IKeyFormatter
		{
			builder.Services.AddTransient<IKeyFormatter, TService>();

			return builder;
		}

		public static IMemcachedConfigurationBuilder UseKeyFormatter(this IMemcachedConfigurationBuilder builder, Func<IServiceProvider, IKeyFormatter> implementationFactory)
		{
			builder.Services.AddTransient(implementationFactory);

			return builder;
		}

		public static IMemcachedConfigurationBuilder UseItemFormatter<TService>(this IMemcachedConfigurationBuilder builder)
			where TService : class, IItemFormatter
		{
			builder.Services.AddTransient<IItemFormatter, TService>();

			return builder;
		}

		public static IMemcachedConfigurationBuilder UseItemFormatter(this IMemcachedConfigurationBuilder builder, Func<IServiceProvider, IItemFormatter> implementationFactory)
		{
			builder.Services.AddTransient(implementationFactory);

			return builder;
		}

		private class TheBuilder : IMemcachedConfigurationBuilder
		{
			public TheBuilder(IServiceCollection services)
			{
				Services = services ?? throw new ArgumentNullException(nameof(services));
			}

			public IServiceCollection Services { get; }
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
