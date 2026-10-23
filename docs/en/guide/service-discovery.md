# Service Registration and Discovery

SyZero uses a unified `IServiceManagement` abstraction for service registration, discovery, health checks, and some leader-election scenarios. You can start with the lightweight built-ins and switch to Redis, Consul, or Nacos later.

## Backend choices

| Backend | Registration API | Good fit |
| --- | --- | --- |
| Local | `AddLocalServiceManagement(builder.Configuration)` | Local development and single-machine integration |
| DB | `AddDBServiceManagement(builder.Configuration)` | Simple multi-instance deployments backed by a database |
| Redis | `AddSyZeroRedis(builder.Configuration)` + `AddRedisServiceManagement(builder.Configuration)` | Lightweight distributed deployments with Pub/Sub updates |
| Consul | `AddConsul(builder.Configuration)` | Standard production-grade service discovery |
| Nacos | `AddNacos(builder.Configuration)` | Environments already using Nacos for registry or config |

## 1. Shared bootstrap

No matter which backend you choose, the host bootstrap should look like this:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddSyZero();

var app = builder.Build();

app.UseSyZero();
app.Run();
```

`builder.AddSyZero()` registers the framework conventions, and `app.UseSyZero()` works with `SyZeroBootstrapper` to register and unregister the service during the app lifecycle.

## 2. Local development with LocalServiceManagement

`LocalServiceManagement` treats `localhost`, loopback addresses, and local network-interface addresses as the same local instance and connects through `localhost`. This prevents duplicate registrations and unreachable health checks caused by Docker or WSL virtual adapters.

This is the lightest option and usually enough for local multi-service testing.

```csharp
builder.Services.AddLocalServiceManagement(options =>
{
    options.EnableHealthCheck = true;
    options.EnableLeaderElection = false;
}, builder.Configuration);
```

If your immediate goal is just to get Feign or a gateway working on your machine, this is typically the best first step.

You can also seed local instances through configuration so that a gateway knows its downstream addresses immediately:

```json
{
  "LocalServiceManagement": {
    "EnableHealthCheck": true,
    "HealthCheckIntervalSeconds": 10,
    "Services": [
      {
        "ServiceName": "SyZero.Example1",
        "ServiceAddress": "localhost",
        "ServicePort": 5002,
        "ServiceProtocol": "HTTP",
        "HealthCheckUrl": "http://localhost:5002/health"
      }
    ]
  }
}
```

Registration data is stored in `syzero_services.json` under the system temporary directory by default and synchronized between local processes through a file watcher. Use Redis, Consul, or Nacos when production instances need cross-machine consistency.

## 3. Database-backed service management

If you need shared registration state without introducing Consul or Nacos yet:

```csharp
builder.Services.AddDBServiceManagement(options =>
{
    options.EnableHealthCheck = true;
    options.EnableLeaderElection = true;
}, builder.Configuration);
```

This is a pragmatic option when:

1. The deployment scale is still moderate
2. A database is the most reliable infrastructure already available
3. You want to avoid an extra registry dependency for now

## 4. Redis-backed service management

Redis is more suitable than DB for lightweight distributed deployments and can push service change notifications through Pub/Sub.

```csharp
builder.Services.AddSyZeroRedis(builder.Configuration);
builder.Services.AddRedisServiceManagement(builder.Configuration, options =>
{
    options.EnableHealthCheck = true;
    options.EnableLeaderElection = true;
    options.EnablePubSub = true;
});
```

Configuration example:

```json
{
  "Redis": {
    "Type": "MasterSlave",
    "Master": "localhost:6379,password=123456,defaultDatabase=0",
    "Slave": []
  },
  "RedisServiceManagement": {
    "EnableHealthCheck": true,
    "EnableLeaderElection": true,
    "EnablePubSub": true
  }
}
```

## 5. Production with Consul

If your platform already uses Consul, the integration is straightforward:

```csharp
builder.Services.AddConsul(builder.Configuration);
```

Configuration example:

```json
{
  "Consul": {
    "ConsulAddress": "http://localhost:8500"
  }
}
```

You can also use the overload that binds configuration first and then applies extra code-based changes.

## 6. Production with Nacos

If Nacos is your registry or configuration center:

```csharp
builder.Services.AddNacos(builder.Configuration);
```

Configuration example:

```json
{
  "Nacos": {
    "ServerAddresses": [ "http://localhost:8848" ],
    "Namespace": "public"
  }
}
```

This path is especially useful if your environment already relies on Nacos config management.

## 7. How this relates to Feign and gateways

Service discovery is usually consumed by one of these higher-level modules:

1. [`SyZero.Feign`](/en/modules/SyZero.Feign) for remote service calls
2. [`SyZero.Gateway.Ocelot`](/en/modules/SyZero.Gateway.Ocelot) or [`SyZero.Gateway.YARP`](/en/modules/SyZero.Gateway.YARP) for gateway routing

Common combinations:

```csharp
builder.AddSyZero();
builder.Services.AddLocalServiceManagement(builder.Configuration);
builder.Services.AddSyZeroFeign(builder.Configuration);
```

or:

```csharp
builder.AddSyZero();
builder.Services.AddConsul(builder.Configuration);
builder.Services.AddSyZeroFeign(builder.Configuration);
```

## 8. Recommended defaults

1. Start local with `AddLocalServiceManagement(builder.Configuration)`
2. Use `AddRedisServiceManagement(builder.Configuration)` or `AddDBServiceManagement(builder.Configuration)` for lightweight distributed deployments
3. Prefer `AddConsul(builder.Configuration)` or `AddNacos(builder.Configuration)` for standard production environments
4. If you are running a single-host app such as `Example3.SPA`, you usually do not need service discovery at all

## 9. Local service-discovery troubleshooting

### Health checks repeat or fail continuously

Open the failed outbound span in Aspire Traces. If it targets a Docker or WSL virtual adapter such as `172.x.x.x:5002`, update to the latest v10 implementation and restart all services. `LocalServiceManagement` normalizes local addresses to `localhost` and keeps one logical instance per protocol and port.

### Feign reports that no healthy instance exists

Check in this order:

1. Caller and callee use exactly the same `ServiceName`.
2. `HealthCheckUrl` is reachable from the caller process and returns 2xx.
3. `SyZero:Port` matches the port on which the service actually listens.
4. The caller registers both `AddLocalServiceManagement(builder.Configuration)` and `AddSyZeroFeign(builder.Configuration)`.
5. Processes were restarted after configuration changes, and you are inspecting new trace records.

### When not to use LocalServiceManagement

It targets development on one machine and single-machine deployments. Containers spanning hosts, Kubernetes, and multi-node production environments should not share registration state through a local temporary file; use Redis, Consul, or Nacos.

## Next

1. Continue with [Event Bus and Integration Events](/en/guide/event-bus)
2. For declarative remote calls, see [SyZero.Feign](/en/modules/SyZero.Feign)
3. For gateway routing, see [SyZero.Gateway.Ocelot](/en/modules/SyZero.Gateway.Ocelot) and [SyZero.Gateway.YARP](/en/modules/SyZero.Gateway.YARP)
4. For Aspire-based integration, see [AppHost Local Orchestration and Observability](/en/guide/apphost-observability)
