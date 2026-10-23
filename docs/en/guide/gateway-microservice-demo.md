# SyZero.Gateway Microservice Gateway Demo

`src/SyZero.Gateway/` is a gateway host sample built with `SyZero.Gateway.Ocelot` and `SyZero.Gateway.Admin`. It is a good front door for services such as `Example1` and `Example2`, giving you one entry point, Swagger aggregation, and an `/admin` UI for managing routes.

The built-in `configuration.json` already contains service-discovery routes for Example1 and Example2, exposed through `/example1/*` and `/example2/*`. To exercise the complete flow from a browser, run the [Example5.Frontend sample](/en/guide/example5-gateway-frontend).

## Recommended: start everything with AppHost

```bash
dotnet run --launch-profile https --project src/SyZero.AppHost/SyZero.AppHost.csproj
```

This starts Gateway, Example1, Example2, Example3, and Example5.Frontend together. The fixed entry points are:

| Entry point | Address |
| --- | --- |
| Aspire Dashboard | `https://localhost:57289` |
| Gateway | `http://localhost:5000` |
| Example5.Frontend | `http://localhost:5175` |

See [AppHost Local Orchestration and Observability](/en/guide/apphost-observability) for the full resource, port, and telemetry workflow.

## Option 1: simplest local setup

This option does not require any registry. The gateway points directly to `localhost:5002` and `localhost:5003`.

### 1. Start the two downstream services

Start:

1. `SyZero.Example1.Web` on `http://localhost:5002`
2. `SyZero.Example2.Web` on `http://localhost:5003`

### 2. Replace `configuration.json` with sample routes

Use a minimal config like this:

```json
{
  "Routes": [
    {
      "UpstreamPathTemplate": "/example1/{everything}",
      "UpstreamHttpMethod": [ "Get", "Post", "Put", "Delete", "Patch" ],
      "DownstreamPathTemplate": "/{everything}",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [
        { "Host": "localhost", "Port": 5002 }
      ],
      "SwaggerKey": "example1"
    },
    {
      "UpstreamPathTemplate": "/example2/{everything}",
      "UpstreamHttpMethod": [ "Get", "Post", "Put", "Delete", "Patch" ],
      "DownstreamPathTemplate": "/{everything}",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [
        { "Host": "localhost", "Port": 5003 }
      ],
      "SwaggerKey": "example2"
    }
  ],
  "SwaggerEndPoints": [
    {
      "Key": "example1",
      "Config": [
        {
          "Name": "SyZero.Example1 API",
          "Version": "v1",
          "Service": {
            "Name": "localhost:5002",
            "Path": "/openapi/v1.json"
          }
        }
      ]
    },
    {
      "Key": "example2",
      "Config": [
        {
          "Name": "SyZero.Example2 API",
          "Version": "v1",
          "Service": {
            "Name": "localhost:5003",
            "Path": "/openapi/v1.json"
          }
        }
      ]
    }
  ]
}
```

This means:

1. `/example1/*` is forwarded to `http://localhost:5002/*`
2. `/example2/*` is forwarded to `http://localhost:5003/*`
3. Swagger aggregation includes both downstream services

### 3. Set a password for `/admin`

The sample `src/SyZero.Gateway/appsettings.json` includes a local development account:

```json
"Gateway": {
  "Admin": {
    "Username": "admin",
    "Password": "admin"
  }
}
```

When `Password` is empty, `/admin` and `/admin-api` return `503`. The sample password is for local development only. Replace it through configuration or environment variables before deployment, and never commit real credentials.

### 4. Start the gateway

```bash
dotnet run --project src/SyZero.Gateway/SyZero.Gateway.csproj
```

The repository launch settings use `http://localhost:5000`, so that is the address to test.

### 5. Verify forwarding

#### Through the gateway to Example1

1. `http://localhost:5000/example1/api/SyZero.Example1/Example1/GetServiceInfo`
2. `http://localhost:5000/example1/api/SyZero.Example1/Example1/CallExample2Service`

#### Through the gateway to Example2

1. `http://localhost:5000/example2/api/SyZero.Example2/Example2/GetServiceInfo`
2. `http://localhost:5000/example2/api/SyZero.Example2/Example2/CallExample1Service`

### 6. Verify the admin UI

After the gateway is running, browse:

1. `http://localhost:5000/admin`
2. `http://localhost:5000/admin/routes`
3. `http://localhost:5000/admin/swagger-endpoints`
4. `http://localhost:5000/admin/service-interfaces`
5. `http://localhost:5000/admin/tester`

This UI is useful for:

1. Editing `Routes` online
2. Editing `SwaggerEndPoints` online
3. Checking whether runtime config diverges from disk config
4. Iterating on local routing without hand-editing JSON every time
5. Inspecting HTTP endpoints reported by registered instances and clicking a Path to hand its method and URL to the route tester

The Service Interfaces page requires the gateway and downstream services to discover each other through the same `IServiceManagement` backend. In this sample, a service reports its downstream path, such as `/api/SyZero.Example1/Example1/GetServiceInfo`. Because the gateway adds the `/example1` upstream prefix, change the tester URL to `/example1/api/SyZero.Example1/Example1/GetServiceInfo` before sending.

## Option 2: registry-backed gateway

If you do not want to hard-code downstream addresses, switch the gateway to SyZero's unified service-discovery abstraction:

1. Register `AddLocalServiceManagement(builder.Configuration)`, `AddConsul(builder.Configuration)`, `AddNacos(builder.Configuration)`, or `AddRedisServiceManagement(builder.Configuration)`
2. Enable `EnableSyZeroServiceDiscovery = true` inside `AddSyZeroOcelot(...)`
3. Change `configuration.json` routes to use `UseServiceDiscovery = true` and `ServiceName = "SyZero.Example1"` style entries

That path is closer to a real microservice deployment. See [SyZero.Gateway.Ocelot](/en/modules/SyZero.Gateway.Ocelot) for the concrete setup.

## Common issues

### Gateway starts but `/admin` returns 503

Check whether `Gateway:Admin:Password` is still empty.

### Gateway starts but forwarding returns 404

Check:

1. `UpstreamPathTemplate` matches the path you are requesting
2. `DownstreamPathTemplate` keeps the required downstream path shape
3. The downstream services are actually running on `5002` and `5003`
4. You did not use `/api/SyZero.Example3/...`; Example3 is a single-host SPA and is not configured in this gateway

For example, do not call:

```text
http://localhost:5000/api/SyZero.Example3/Example3/GetServiceInfo
```

A valid gateway request starts with `/example1` or `/example2`.

### Downstream services work but Swagger aggregation fails

Check:

1. `SwaggerEndPoints[*].Config[*].Service.Path` is `/openapi/v1.json`
2. Each downstream service can expose its Swagger document directly
3. Whether a gateway restart is still required after Swagger endpoint edits

### The Service Interfaces page is empty

Check:

1. The gateway and downstream services register the same `IServiceManagement` implementation and connect to the same registry backend
2. The downstream services execute `app.UseSyZero()` so service registration completes
3. The downstream services run a version that publishes endpoint metadata; restart them after upgrading so they register again

## Next

1. Continue with [Example1 + Example2 Microservice Call Demo](/en/guide/example1-example2-microservices)
2. Run the [Example5.Frontend Gateway Client Sample](/en/guide/example5-gateway-frontend)
3. Read [AppHost Local Orchestration and Observability](/en/guide/apphost-observability)
4. Continue with [Service Registration and Discovery](/en/guide/service-discovery)
5. Continue with [SyZero.Gateway.Admin](/en/modules/SyZero.Gateway.Admin)
