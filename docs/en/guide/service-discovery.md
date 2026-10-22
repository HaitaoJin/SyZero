# Service Registration and Discovery

SyZero uses a unified `IServiceManagement` abstraction for service registration, discovery, health checks, and some leader-election scenarios. You can start with the lightweight built-ins and switch to Redis, Consul, or Nacos later.

## Backend choices

| Backend | Registration API | Good fit |
| --- | --- | --- |
| Local | `AddLocalServiceManagement()` | Local development and single-machine integration |
| DB | `AddDBServiceManagement()` | Simple multi-instance deployments backed by a database |
| Redis | `AddSyZeroRedis()` + `AddRedisServiceManagement()` | Lightweight distributed deployments with Pub/Sub updates |
| Consul | `AddConsul()` | Standard production-grade service discovery |
| Nacos | `AddNacos()` | Environments already using Nacos for registry or config |

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

This is the lightest option and usually enough for local multi-service testing.

```csharp
builder.Services.AddLocalServiceManagement(options =>
{
    options.EnableHealthCheck = true;
    options.EnableLeaderElection = false;
});
```

If your immediate goal is just to get Feign or a gateway working on your machine, this is typically the best first step.

## 3. Database-backed service management

If you need shared registration state without introducing Consul or Nacos yet:

```csharp
builder.Services.AddDBServiceManagement(options =>
{
    options.EnableHealthCheck = true;
    options.EnableLeaderElection = true;
});
```

This is a pragmatic option when:

1. The deployment scale is still moderate
2. A database is the most reliable infrastructure already available
3. You want to avoid an extra registry dependency for now

## 4. Redis-backed service management

Redis is more suitable than DB for lightweight distributed deployments and can push service change notifications through Pub/Sub.

```csharp
builder.Services.AddSyZeroRedis(builder.Configuration);
builder.Services.AddRedisServiceManagement(options =>
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
builder.Services.AddConsul();
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
builder.Services.AddNacos();
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
builder.Services.AddLocalServiceManagement();
builder.Services.AddSyZeroFeign();
```

or:

```csharp
builder.AddSyZero();
builder.Services.AddConsul();
builder.Services.AddSyZeroFeign();
```

## 8. Recommended defaults

1. Start local with `AddLocalServiceManagement()`
2. Use `AddRedisServiceManagement()` or `AddDBServiceManagement()` for lightweight distributed deployments
3. Prefer `AddConsul()` or `AddNacos()` for standard production environments
4. If you are running a single-host app such as `Example3.SPA`, you usually do not need service discovery at all

## Next

1. Continue with [Event Bus and Integration Events](/en/guide/event-bus)
2. For declarative remote calls, see [SyZero.Feign](/en/modules/SyZero.Feign)
3. For gateway routing, see [SyZero.Gateway.Ocelot](/en/modules/SyZero.Gateway.Ocelot) and [SyZero.Gateway.YARP](/en/modules/SyZero.Gateway.YARP)
