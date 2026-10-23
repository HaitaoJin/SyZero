# Example1 + Example2 Microservice Call Demo

`SyZero.Example1.Service` and `SyZero.Example2.Service` are the most direct microservice samples in this repository. Both expose APIs through `DynamicWebApi`, call each other through `SyZero.Feign`, and use `LocalServiceManagement` as the local discovery backend.

## Topology

```text
SyZero.Example1.Web  http://localhost:5002
  ├─ DynamicWebApi
  ├─ SqlSugar + SQLite
  ├─ LocalServiceManagement
  └─ Feign -> SyZero.Example2

SyZero.Example2.Web  http://localhost:5003
  ├─ DynamicWebApi
  ├─ SqlSugar + SQLite
  ├─ LocalServiceManagement
  └─ Feign -> SyZero.Example1
```

## What is already configured

These sample services already ship with:

1. `Example1` on port `5002`, service name `SyZero.Example1`
2. `Example2` on port `5003`, service name `SyZero.Example2`
3. `AddDynamicWebApi(...)` enabled on both sides
4. `AddSyZeroFeign(builder.Configuration)` enabled on both sides
5. `AddLocalServiceManagement(builder.Configuration)` enabled on both sides
6. The peer service declared in each `Feign:Service` section

That means you can demonstrate two-service remote calls locally without setting up Consul or Nacos first.

You can also start both services, Gateway, and the frontend through AppHost:

```bash
dotnet run --launch-profile https --project src/SyZero.AppHost/SyZero.AppHost.csproj
```

## 1. Start Example1

```bash
dotnet run --project src/SyZero.Service/SyZero.Example1.Service/SyZero.Example1.Web/SyZero.Example1.Web.csproj
```

Then browse:

1. `http://localhost:5002/openapi/v1.json`
2. `http://localhost:5002/api/SyZero.Example1/Example1/GetServiceInfo`

## 2. Start Example2

```bash
dotnet run --project src/SyZero.Service/SyZero.Example2.Service/SyZero.Example2.Web/SyZero.Example2.Web.csproj
```

Then browse:

1. `http://localhost:5003/openapi/v1.json`
2. `http://localhost:5003/api/SyZero.Example2/Example2/GetServiceInfo`

## 3. Verify cross-service calls

After both services are running, call these endpoints directly.

### From Example1 to Example2

1. `GET http://localhost:5002/api/SyZero.Example1/Example1/CallExample2Service`
2. `GET http://localhost:5002/api/SyZero.Example1/Example1/GetExample2Data?id=1`

### From Example2 to Example1

1. `GET http://localhost:5003/api/SyZero.Example2/Example2/CallExample1Service`
2. `GET http://localhost:5003/api/SyZero.Example2/Example2/GetExample1Data?id=1`

These calls demonstrate:

1. Fetching peer service metadata
2. Invoking peer business APIs through Feign
3. Returning DTOs backed by peer-side sample data

## 4. Key implementation points

### DynamicWebApi

Both services set `DefaultAreaName` to their service name, so the route prefixes are:

1. `Example1` -> `/api/SyZero.Example1/...`
2. `Example2` -> `/api/SyZero.Example2/...`

### Feign

`Example1` already declares:

```json
"Feign": {
  "Service": [
    {
      "ServiceName": "SyZero.Example2",
      "AssemblyName": "SyZero.Example2.IApplication",
      "Protocol": "Http"
    }
  ]
}
```

`Example2` declares the reverse dependency on `SyZero.Example1`.

### Service discovery

Local development uses `LocalServiceManagement` by default. In practice this means:

1. No external registry is required
2. Each service registers itself when started
3. Feign resolves peer instances through the shared `IServiceManagement` abstraction

## 5. When to switch to Consul or Nacos

If you want to evolve this into a more production-like topology:

1. Replace `AddLocalServiceManagement(builder.Configuration)` with `AddConsul(builder.Configuration)` or `AddNacos(builder.Configuration)`
2. Keep `AddSyZeroFeign(builder.Configuration)` unchanged
3. Keep the same `ServiceName` values

That is the intended SyZero design: keep business code stable while swapping the discovery backend.

## 6. Common issues

### Peer call fails

Check these first:

1. Both services are actually running
2. Ports `5002` and `5003` are free
3. The full DynamicWebApi route prefix is correct
4. `Feign:Service[*].ServiceName` still matches the peer `SyZero:Name`

### Swagger works but remote calls fail

That usually means:

1. `DynamicWebApi` is fine
2. The problem is in Feign or service discovery

Compare the local `SyZero:Name` values with the peer names in `Feign:Service`.

## Next

1. To place both services behind one entry point, continue with [SyZero.Gateway Microservice Gateway Demo](/en/guide/gateway-microservice-demo)
2. To switch to a registry-backed setup, continue with [Service Registration and Discovery](/en/guide/service-discovery)
3. For Feign-specific details, read [SyZero.Feign](/en/modules/SyZero.Feign)
4. To inspect the complete distributed trace, read [AppHost Local Orchestration and Observability](/en/guide/apphost-observability)
