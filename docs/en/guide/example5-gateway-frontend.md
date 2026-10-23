# Example5.Frontend Gateway Client Sample

`src/SyZero.Service/SyZero.Example5.Frontend/` is a standalone TypeScript + Vite client that demonstrates browser calls through `SyZero.Gateway` to Example1 and Example2. It has no back-end host and does not proxy Example3; Example3 remains the single-host SPA sample.

## Request flow

```text
Browser → Example5 Vite → SyZero.Gateway :5000 → Example1 :5002 / Example2 :5003
```

The client has two groups of requests. Service status requests validate gateway forwarding:

```text
/example1/api/SyZero.Example1/Example1/GetServiceInfo
/example2/api/SyZero.Example2/Example2/GetServiceInfo
```

Service-to-service requests continue from the downstream service into its peer:

```text
/example1/api/SyZero.Example1/Example1/CallExample2Service
/example2/api/SyZero.Example2/Example2/CallExample1Service
```

The complete paths are:

```text
Browser → Vite → Gateway → Example1
Browser → Vite → Gateway → Example2
Browser → Vite → Gateway → Example1 → Example2
Browser → Vite → Gateway → Example2 → Example1
```

## Run with AppHost

```bash
dotnet run --project src/SyZero.AppHost/SyZero.AppHost.csproj
```

Aspire orchestrates Example1, Example2, Example3, Gateway, and `example5-frontend`. The Dashboard is fixed at `https://localhost:57289`, and the client is fixed at `http://localhost:5175`. After **Refresh service status**, both service cards should be healthy. After **Call all**, both service-to-service calls should succeed.

`example5-frontend` uses `.WaitFor(gateway)`, so it starts only after Gateway is ready. The Aspire Resources page does not keep a separate completed `npm install` resource.

## Run the client separately

Start Example1, Example2, and Gateway on ports `5002`, `5003`, and `5000`, then run:

```bash
cd src/SyZero.Service/SyZero.Example5.Frontend
npm install
npm run dev
```

The default URL is `http://localhost:5175`. Vite proxies `/example1` and `/example2` to `http://localhost:5000`.

Vite enables `strictPort`. If port `5175` is occupied, startup fails with an explicit conflict instead of silently selecting another port.

## UI actions and expected results

| Action | What it validates | Success condition |
| --- | --- | --- |
| Refresh service status | Gateway routing to Example1 and Example2 | `2 / 2` services are reachable through the gateway |
| Example1 → Example2 | Example1 Feign discovery and remote call | Success, with Example2 shown as the remote service |
| Example2 → Example1 | Example2 Feign discovery and remote call | Success, with Example1 shown as the remote service |
| Call all | Both service-to-service paths in parallel | Both call cards succeed |

Every request uses a relative path. The browser always calls the same-origin Vite server, which forwards requests to Gateway, so the client does not hard-code a cross-origin URL.

## Build verification

```bash
npm run build
```

The build performs strict TypeScript checking before emitting static files to `dist/`.

## Troubleshooting

### The page returns HTTP 404

Verify that the path starts with `/example1` or `/example2`. Example3 is a single-host SPA sample; it is not a gateway route and should not be added to Example5.

### The page opens, but both services are unavailable

Confirm that Gateway, Example1, and Example2 are running on ports `5000`, `5002`, and `5003`, then call:

```text
http://localhost:5000/example1/api/SyZero.Example1/Example1/GetServiceInfo
http://localhost:5000/example2/api/SyZero.Example2/Example2/GetServiceInfo
```

### Service status works, but service-to-service calls fail

The browser-to-gateway path is healthy, so the failure is in Feign or service discovery between Example1 and Example2. Inspect the Aspire **Traces** page for `gateway → example1 → example2` or the reverse path, then follow [Service Registration and Discovery](/en/guide/service-discovery).

## Next

- [AppHost Local Orchestration and Observability](/en/guide/apphost-observability)
- [SyZero.Gateway Microservice Gateway Demo](/en/guide/gateway-microservice-demo)
