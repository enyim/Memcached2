# Enyim.Memcached2

A fully async, pipelining, high-performance [Memcached](https://memcached.org/) library for .NET

(A full rewrite of [Enyim.Memcached](https://github.com/enyim/EnyimMemcached).)

## Limitations

- Only the binary protocol is supporrted (unless using a very old Memcached server, this should not be a blocker)
- Uses `Memory<T>`/`Span<T>`, so currently it only runs on .NET Core 3.0 (netstandard2.1)

# Installation

### .NET CLI

``` shell
dotnet add package Enyim.Memcached2 --version 0.6.0-preview
```

### PackageReference

``` xml
<PackageReference Include="Enyim.Memcached2" Version="0.6.0-preview" />
```

# Usage

``` csharp
static async Task Main(string[] args)
{
    // connect to a list of memcached servers
    var cluster = new MemcachedCluster("localhost,localhost:11212,localhost:11213");
    // this is mandatory
    cluster.Start();

    var client = cluster.GetClient();
    await client.SetAsync("hello", new { hello= "world" });

    // more work

    // stop the cluster
    cluster.Dispose();
}
```

## The MemcachedCluster

``` csharp
var cluster = new MemcachedCluster("localhost,localhost:11212,localhost:11213");
cluster.Start();
```

Memcached2 has no `SocketPool`, like the previous version: it maintains a persistent connection to each server. It also handles all communications (sending operations, parsing replies) with all servers via its own IO Thread.

Because of this, it should be used as a singleton, or at least a "long-lived instance":

- initialize at the start of the application ("bootstrapping" phase)
- `Dispose` it when quitting (or when it's not needed anymore)

Each cluster can only be used to submit work to the servers specified at construction: for each group of servers a new instance must be created (and kept alive until the application exits).

## The MemcachedClient

``` csharp
var client = cluster.GetClient();
```

The MemcachedClient is used for performing the operations.

The `GetClient` (without parameters) is cheap to call, there is no need to save the returned instance. (It is cached by the cluster.) 

When using an IoC container it is recommended to register it as a singleton.

``` csharp
services.AddSingleton<IMemcachedClient>(cluster.GetClient())
```

Or, if the Cluster is also registered:

``` csharp
services.AddSingleton<IMemcachedClient>(ctx => ctx.GetRequiredService<IMemcachedCluster>().GetClient())
```

**Note**: if the Cluster is not started before the `GetClient` is called an exception will be thrown.

# Advanced Usage

## Customizing the MemcachedCluster

The constructor accepts a `IMemcachedClusterOptions` instance:

``` csharp
public interface IMemcachedClusterOptions
{
    MemoryPool<byte> Allocator { get; }
    INodeLocator Locator { get; }
    IReconnectPolicyFactory ReconnectPolicyFactory { get; }
    IFailurePolicyFactory FailurePolicyFactory { get; }
    ISocketFactory SocketFactory { get; }
}
```

There is also a default implementation:

``` csharp
var cluster = new MemcachedCluster(hosts, new MemcachedClusterOptions
{
    // ...
});
```

### Allocator

Used for allocating buffers (byte arrays) to reduce the GC pressure.

Defaults to `MemoryPool<byte>.Shared`.

## Locator

Implement the `INodeLocator` interface to define how the items (keys) are mapped to the Memcached servers in the cluster.

The default `DefaultNodeLocator` uses Ketama consistent hashing with a [Murmurhash3](https://github.com/aappleby/smhasher/wiki/MurmurHash3) hash function.

## ReconnectPolicyFactory

Implement the `IReconnectPolicy` and its `Factory` to control how the Cluster reconnects to failed nodes.

The default `PeriodicReconnectPolicyFactory` reconnects every 10 seconds.

``` csharp
var cluster = new MemcachedCluster(hosts, new MemcachedClusterOptions
{
    ReconnectPolicyFactory = new PeriodicReconnectPolicyFactory { Interval = TimeSpan.FromSeconds(10) }
});
```

## FailurePolicyFactory

Implement the `IFailurePolicy` and its `Factory` to control how the Cluster fails nodes when a socket error occurs. 

The default `ImmediateFailurePolicyFactory` fails the node at the first error.

Some firewalls abort long-open TCP connections, no matter if the connection is still valid. In this case you can implement a custom policy which 

1. tries to immediately reconnect when a socket error occurs _(possibly the firewall caused it)_
2. fails the node if a second error occurs in a given timeframe _(the node is probably down)_

If the network connection to the server(s) is not stable, the `ThrottlingFailurePolicyFactory` can help, as it only fails a node when a given amount of failures occur in a configurable time window. (E.g. 3 fails in 30 secs.)

``` csharp
var cluster = new MemcachedCluster(hosts, new MemcachedClusterOptions
{
    FailurePolicyFactory = new ThrottlingFailurePolicyFactory { ResetAfter = TimeSpan.FromSeconds(30), Threshold = 3 }
});
```

However, it is a better long-term solution just making the connection more stable.

## SocketFactory

The main use-case is to customize the behavior of the Sockets used by the Cluster.

``` csharp
var cluster = new MemcachedCluster(hosts, new MemcachedClusterOptions
{
    SocketFactory = new AsyncSocketFactory (new SocketOptions
    {
        ConnectionTimeout = TimeSpan.FromSeconds(20)
    })
});
```

See the [SocketOptions](https://github.com/enyim/Memcached2/blob/master/src/Enyim.Caching/SocketOptions.cs) and [AsyncSocket](https://github.com/enyim/Memcached2/blob/master/src/Enyim.Caching/AsyncSocket.cs) for further options and their default values.

## Customizing the MemcachedClient

Some aspects of the Client's behavior can be customized using the `IMemcachedClientOptions`.

``` csharp
public interface IMemcachedClientOptions
{
    IKeyFormatter KeyFormatter { get; }
    IItemFormatter ItemFormatter { get; }
}
```

Pass an instance to the MemcachedCluster's constructor to customize the **default** instance. (The instance returned by `GetClient`.)

``` csharp
var cluster = new MemcachedCluster(hosts, clientOptions: new MemcachedClientOptions
{
    // ...
});
```

Pass an instance to `MemcachedCluster.GetClient()` to get a customized _instance_.

**Note**: these Client instances are not cached by the Cluster

``` csharp
var client = cluster.GetClient(new MemcachedClientOptions
{
});
```

The same can be achieved by just creating a new MemcachedClient instance:

``` csharp
var client = new MemcachedClient(cluster, new MemcachedClientOptions
{
});
```

There is no difference between the two options (GetClient also uses the constructor), pick one based on personal preference.

## KeyFormatter

Implement the `IKeyFormatter` interface to control how the _item keys_ are serialized to `byte[]` (so that they can be sent to Memcached).

The default `Utf8KeyFormatter` converts the keys to their UTF-8 representation.

Use the `NamespacingKeyFormatter` to make the Client prefix all item keys with a string, e.g. when multiple applications use the same keys but their data should be separate.

``` csharp
var client = cluster.GetClient(new MemcachedClientOptions
{
    KeyFormatter = new NamespacingKeyFormatter("customprefix:")
});
```

## ItemFormatter

Implement the `ItemFormatter` interface to control how the _items_ are serialized to `byte[]` (so that they can be sent to Memcached).

The default `BinaryItemFormatter` serializes items the following way:

- numbers are sent as their [big-endian](https://en.wikipedia.org/wiki/Endianness#Big-endian) representation 
- `bool`: 0/1
- `DateTime` as `long` (`.ToBinary()`)
- `string` as UTF-8 bytes
- anything else is serialized using `System.Runtime.Serialization.Formatters.Binary.BinaryFormatter`

# Diagnostics

The client supports emitting its diagnostics messages to the Console or some of the popular logging libraries. (Internal logging is disabled if no diagnostics logger is installed.)

The logger musy be installed before creating the CLuster:
``` csharp
var factory = ...;
Enyim.LogManager.AssignFactory(factory);
```

## Console logging

The Console logger is built-in.

``` csharp
var factory = ...;
Enyim.LogManager.AssignFactory(new Enyim.Diagnostics.ConsoleLoggerFactory(LogLevel.Information));
```

## Serilog

Requires a Nuget package:

``` shell
dotnet add package Enyim.Diagnostics.Serilog --version 0.6.0-preview
```

Usage:

``` csharp
Enyim.LogManager.AssignFactory(new Enyim.Diagnostics.SerilogLoggerFactory());
```

If no `Serilog.LoggerConfiguration` is passed to the constructor the default `Serilog.Log.Logger` will be used (i.e. the global log configuration.)

## NLog

Requires a Nuget package:

``` shell
dotnet add package Enyim.Diagnostics.NLog --version 0.6.0-preview
```

Usage:

``` csharp
Enyim.LogManager.AssignFactory(new Enyim.Diagnostics.NLogLoggerFactory());
```

If no `NLog.LogFactory` is passed to the constructor the default `NLog.LogManager.LogFactory` will be used (i.e. the global log configuration.)

## Microsoft.Extensions.Logging

Requires a Nuget package:

``` shell
dotnet add package Enyim.Diagnostics.ExtensionsLogging --version 0.6.0-preview
```

Usage:

``` csharp

/* 
// application specific code
var loggerFactory = new LoggerFactory(...)

// or 
var loggerFactory = app.ApplicationServices.GetService<Microsoft.Extensions.Logging.LoggerFactory>();
*/

Enyim.LogManager.AssignFactory(new Enyim.Diagnostics.MicrosoftLoggerFactory(loggerFactory));
```

# Extensions

## Asp.NET Core

``` shell
dotnet add package Enyim.Memcached2.Extensions.AspNetCore --version 0.6.0-preview
```

Use the AddMemcached extension method to

- configure the Cluster
- add the Client to the IoC container
- start the Cluster during application startup

``` csharp
public class Startup
{
    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        // ...

        services
            .AddMemcached("localhost")
            .UseKeyFormatter(_ => new NamespacingKeyFormatter("customprefix:"));
    }
}
```

Now the `IMemcachedCluser` and `IMemcachedClient` can be injected into pages/controllers:

``` razor
@inject Enyim.Caching.Memcached.IMemcachedClient client
```

``` csharp
public class IndexModel: PageModel
{
    public IndexModel(IMemcachedCluster cluster)
    {
    }
}
```

See the [source code](https://github.com/enyim/Memcached2/blob/master/src/Extensions/AspNetCore/MemcachedConfigurationBuilderExtensions.cs) for further customization options.

