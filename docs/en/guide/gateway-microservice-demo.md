# SyZero.Gateway Microservice Gateway Demo

`src/SyZero.Gateway/` is a gateway host sample built with `SyZero.Gateway.Ocelot` and `SyZero.Gateway.Admin`. It is a good front door for services such as `Example1` and `Example2`, giving you one entry point, Swagger aggregation, and an `/admin` UI for managing routes.

## Important current state

The repository's built-in [configuration.json](/d:/Project/syzero/SyZero/src/SyZero.Gateway/configuration.json) currently targets service names such as `SyZero.AI` and `SyZero.Authorization`, not `Example1` or `Example2`.

So if you want to use the gateway with the sample microservices, replace that configuration first.

## Option 1: simplest local setup

This option does not require any registry. The gateway points directly to `localhost:5002` and `localhost:5003`.

### 1. Start the two downstream services

Start:

1. `SyZero.Example1.Web` on `http://localhost:5002`
2. `SyZero.Example2.Web` on `http://localhost:5003`

## 2. Replace `configuration.json` with sample routes

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
            "Path": "/swagger/v1/swagger.json"
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
            "Path": "/swagger/v1/swagger.json"
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

## 3. Set a password for `/admin`

By default, [src/SyZero.Gateway/appsettings.json](/d:/Project/syzero/SyZero/src/SyZero.Gateway/appsettings.json) contains:

```json
"Gateway": {
  "Admin": {
    "Username": "admin",
    "Password": ""
  }
}
```

When `Password` is empty, `/admin` and `/admin-api` return `503`. For local testing, set one, for example:

```json
"Gateway": {
  "Admin": {
    "Username": "admin",
    "Password": "admin123456"
  }
}
```

## 4. Start the gateway

```bash
dotnet run --project src/SyZero.Gateway/SyZero.Gateway.csproj
```

The repository launch settings use `http://localhost:5000`, so that is the address to test.

## 5. Verify forwarding

### Through the gateway to Example1

1. `http://localhost:5000/example1/api/SyZero.Example1/Example1/GetServiceInfo`
2. `http://localhost:5000/example1/api/SyZero.Example1/Example1/CallExample2Service`

### Through the gateway to Example2

1. `http://localhost:5000/example2/api/SyZero.Example2/Example2/GetServiceInfo`
2. `http://localhost:5000/example2/api/SyZero.Example2/Example2/CallExample1Service`

## 6. Verify the admin UI

After the gateway is running, browse:

1. `http://localhost:5000/admin`
2. `http://localhost:5000/admin/routes`
3. `http://localhost:5000/admin/swagger-endpoints`

This UI is useful for:

1. Editing `Routes` online
2. Editing `SwaggerEndPoints` online
3. Checking whether runtime config diverges from disk config
4. Iterating on local routing without hand-editing JSON every time

## Option 2: registry-backed gateway

If you do not want to hard-code downstream addresses, switch the gateway to SyZero's unified service-discovery abstraction:

1. Register `AddLocalServiceManagement()`, `AddConsul()`, `AddNacos()`, or `AddRedisServiceManagement()`
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

### Downstream services work but Swagger aggregation fails

Check:

1. `SwaggerEndPoints[*].Config[*].Service.Path` is `/swagger/v1/swagger.json`
2. Each downstream service can expose its Swagger document directly
3. Whether a gateway restart is still required after Swagger endpoint edits

## Next

1. Continue with [Example1 + Example2 Microservice Call Demo](/en/guide/example1-example2-microservices)
2. Continue with [Service Registration and Discovery](/en/guide/service-discovery)
3. Continue with [SyZero.Gateway.Admin](/en/modules/SyZero.Gateway.Admin)
