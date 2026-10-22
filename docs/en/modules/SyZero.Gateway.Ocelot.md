# SyZero.Gateway.Ocelot

SyZero.Gateway.Ocelot provides API gateway capabilities on top of Ocelot, including dynamic routing, load balancing, and Swagger aggregation.

## Install

```bash
dotnet add package SyZero.Gateway.Ocelot
```

## Key Features

- Ocelot-based upstream/downstream routing
- Load balancing support
- Swagger aggregation for microservices
- Circuit breaking and retry support

## Quick Start

```csharp
builder.Services.AddSyZeroOcelot();
await app.UseSyZeroOcelotAsync();
```

## Configuration Example (appsettings.json)

```json
{
	"Routes": [
		{
			"DownstreamPathTemplate": "/api/users/{everything}",
			"UpstreamPathTemplate": "/gateway/users/{everything}",
			"LoadBalancerOptions": {
				"Type": "RoundRobin"
			}
		}
	]
}
```

## Common Configuration

- Routes
- DownstreamPathTemplate
- UpstreamPathTemplate
- LoadBalancerOptions

## Unified Service Discovery (Consul / Nacos / Redis / Local / DB)

In addition to the built-in `Ocelot.Provider.Consul`, this module ships `SyZeroDiscovery` — a generic Ocelot
service-discovery provider that consumes the `IServiceManagement` abstraction from `SyZero.Core`. Any backend
that registers `IServiceManagement` (Consul / Nacos / Redis / Local / DB) plugs into Ocelot transparently and
shares the same abstraction as [SyZero.Gateway.YARP service discovery](/en/modules/SyZero.Gateway.YARP#service-discovery-integration-consul--nacos--redis--local--db).

```csharp
builder.Services.AddNacos(builder.Configuration);   // or AddConsul / AddRedisServiceManagement / Local / DB
builder.Services.AddSyZeroOcelot(opts =>
{
    opts.EnableConsul = false;                      // disable built-in Ocelot.Provider.Consul if desired
    opts.EnableSyZeroServiceDiscovery = true;       // enable SyZeroDiscovery
}, builder.Configuration);
```

```json
{
  "GlobalConfiguration": {
    "ServiceDiscoveryProvider": { "Type": "SyZeroDiscovery" }
  },
  "Routes": [
    {
      "DownstreamPathTemplate": "/{everything}",
      "DownstreamScheme": "http",
      "ServiceName": "user-service",
      "UpstreamPathTemplate": "/user-service/{everything}",
      "UpstreamHttpMethod": [ "Get", "Post", "Put", "Delete" ],
      "LoadBalancerOptions": { "Type": "RoundRobin" }
    }
  ]
}
```

> Ocelot 23's `ServiceDiscoveryProviderFactory` matches the provider via `provider.GetType().Name` against
> the `Type` field (case-insensitive), so the class is intentionally named `SyZeroDiscovery`.

### Load balancing & health checks

- Load-balancer types follow Ocelot's native names (`RoundRobin` / `LeastConnection` / `NoLoadBalancer` / `CookieStickySessions`) and map 1:1 to the [SyZero.Gateway.YARP LB table](/en/modules/SyZero.Gateway.YARP#load-balancing--health-checks).
- Health filtering happens at the discovery source (`IServiceManagement.GetHealthyServices(...)` only returns healthy instances). Ocelot does not run a second active probe, avoiding duplicate health judgments. If you want Ocelot's native `Provider.Consul` active probing, also enable `EnableConsul = true` to use the original Consul path in parallel.

## References

- Chinese documentation: [/modules/SyZero.Gateway.Ocelot](/modules/SyZero.Gateway.Ocelot)


