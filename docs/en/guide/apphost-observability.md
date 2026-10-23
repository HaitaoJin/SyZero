# AppHost Local Orchestration and Observability

`src/SyZero.AppHost/` uses .NET Aspire to start the sample services, gateway, and front end together, then collects structured logs, distributed traces, and metrics in the Aspire Dashboard. It is the recommended entry point for validating the SyZero microservice call chain.

## Resources and fixed ports

| Resource | Address | Purpose |
| --- | --- | --- |
| Aspire Dashboard | `https://localhost:57289` | Resources, logs, traces, and metrics |
| Dashboard HTTP | `http://localhost:57288` | Dashboard without HTTPS |
| Gateway | `http://localhost:5000` | Shared entry point for Example1 and Example2 |
| Example1 | `http://localhost:5002` | First microservice sample |
| Example2 | `http://localhost:5003` | Second microservice sample |
| Example3 | `http://localhost:5004` | Single-host SPA backend |
| Example3 Vite | `http://localhost:5174` | Development SPA for Example3 |
| Example5.Frontend | `http://localhost:5175` | Standalone gateway client sample |

The Dashboard OTLP receiver is fixed at port `57290`, and the resource service is fixed at `57291`. These ports are defined in `SyZero.AppHost/Properties/launchSettings.json`. Example5 also enables Vite `strictPort`, so a port conflict fails clearly instead of silently selecting a random port.

## Start the application

.NET 10 SDK and Node.js are required. You do not need to start each sample separately:

```bash
dotnet run --launch-profile https --project src/SyZero.AppHost/SyZero.AppHost.csproj
```

The browser opens `https://localhost:57289`. The Resources page should contain:

- `example1`
- `example2`
- `example3`
- `gateway`
- `example5-frontend`

`example5-frontend` is represented by one long-running resource. Dependency installation is startup preparation and does not remain as a separate completed `npm install` resource.

## Verify the complete call chain

Open `http://localhost:5175`, then use **Refresh service status** and **Call all**. You can also call the four gateway endpoints directly:

```text
GET http://localhost:5000/example1/api/SyZero.Example1/Example1/GetServiceInfo
GET http://localhost:5000/example2/api/SyZero.Example2/Example2/GetServiceInfo
GET http://localhost:5000/example1/api/SyZero.Example1/Example1/CallExample2Service
GET http://localhost:5000/example2/api/SyZero.Example2/Example2/CallExample1Service
```

The first two validate browser-to-gateway-to-service routing. The latter two also invoke the Feign calls between Example1 and Example2. Every successful request returns HTTP 200.

## Inspect logs, traces, and metrics

Sample hosts use both parts of the telemetry setup:

```csharp
builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes = true;
});

builder.Services.AddSyZeroOpenTelemetry(builder.Configuration);
```

When AppHost starts a resource, Aspire injects the standard OTLP environment variables, so applications do not need to hard-code the Dashboard address in `appsettings.json`.

### Structured logs

Open **Structured logs** and select `gateway`, `example1`, or `example2`. Requests should produce entries. If the current minimum level is `Warning`, regular `Information` entries are filtered; adjust `Logging:LogLevel` in development settings when needed.

### Distributed traces

Open **Traces**, then invoke both service-to-service calls from Example5. Complete traces normally contain:

```text
gateway → example1 → example2
gateway → example2 → example1
```

Open a trace to inspect each inbound request and outbound HTTP call, including duration, status code, and TraceId.

### Metrics

Open **Metrics**, select a backend resource, and then choose an ASP.NET Core, Kestrel, or `System.Net.Http` metric. Metrics need request samples; an empty list immediately after startup is expected until traffic is generated.

## Troubleshooting

### Dashboard address changes on every start

Use the repository's `https` or `http` launch profile instead of temporary host arguments:

```bash
dotnet run --launch-profile https --project src/SyZero.AppHost/SyZero.AppHost.csproj
```

The fixed addresses are `https://localhost:57289` and `http://localhost:57288`.

### Logs, traces, or metrics are empty

Check in this order:

1. The target resource is `Running` on the Resources page.
2. You have called an API to generate traffic.
3. The application registers both the OpenTelemetry logging provider and `AddSyZeroOpenTelemetry(...)`.
4. The application was started by AppHost. A standalone launch requires `OpenTelemetry:OtlpUrl` or `OTEL_EXPORTER_OTLP_ENDPOINT`.
5. `AspNetCorePathPrefixes` / `HttpClientPathPrefixes` do not exclude the current request.

### Repeated red `gateway: GET` traces

This usually means gateway health checks are calling an unreachable registered address. In v10, `LocalServiceManagement` treats `localhost`, loopback addresses, and local network interfaces as one instance and performs local checks through `localhost`, avoiding duplicate failures caused by Docker or WSL virtual adapters. Restart AppHost after upgrading. Red entries collected before the restart are historical data and remain visible.

### Two red `example3: GET` traces appear during startup

Example3 is a single-host SPA sample. During the first probes to `localhost:5174`, Vite may still be starting, leaving a small number of roughly two-second failures. Probes succeed once Vite is ready. This is a one-time startup condition and should not repeat.

### Gateway request returns 404

Do not use an Example3 route through the gateway. The current gateway exposes only `/example1/*` and `/example2/*`. Confirm that Gateway is running on port `5000` and use one of the complete paths listed above.

## Next

- [Example5.Frontend Gateway Client Sample](/en/guide/example5-gateway-frontend)
- [SyZero.Gateway Microservice Gateway Demo](/en/guide/gateway-microservice-demo)
- [Service Registration and Discovery](/en/guide/service-discovery)
- [SyZero.Observability.OpenTelemetry](/en/modules/SyZero.Observability.OpenTelemetry)
