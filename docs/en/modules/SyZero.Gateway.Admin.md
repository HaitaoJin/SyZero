# SyZero.Gateway.Admin

Visual admin panel for the SyZero gateway abstraction. It depends only on the abstractions defined in [`SyZero.Gateway.Abstractions`](./SyZero.Gateway.Abstractions): `IGateway`, `IGatewayConfigurationStore`, and `IGatewayConfigurationApplier`. It is not tied to a specific backend, so it can work with [`SyZero.Gateway.Ocelot`](./SyZero.Gateway.Ocelot), [`SyZero.Gateway.YARP`](./SyZero.Gateway.YARP), or any future `IGateway` implementation.

Once mounted into a host application, it exposes a Razor Pages UI at `/admin` and REST endpoints under `/admin-api/*` for CRUD operations on gateway `Routes` and `SwaggerEndPoints`. Persistence is delegated to the registered `IGatewayConfigurationStore`, and every write is then pushed to the runtime through `IGatewayConfigurationApplier` for hot reload. If the host does not register a concrete store, the module falls back to `FileGatewayConfigurationStore` (file write + backups, keeping 10 snapshots by default).

The UI uses [Tailwind CSS](https://cdn.tailwindcss.com) and [daisyUI](https://daisyui.com) from CDNs, so no frontend build pipeline is required.

## Install

```bash
dotnet add package SyZero.Gateway.Admin
```

> Your gateway host should also enable one concrete `IGateway` backend such as [`SyZero.Gateway.Ocelot`](./SyZero.Gateway.Ocelot) or [`SyZero.Gateway.YARP`](./SyZero.Gateway.YARP).

## Quick start

```csharp
using SyZero.Gateway.Ocelot;
using SyZero.Gateway.Admin;

var builder = WebApplication.CreateBuilder(args);

builder.AddSyZero();
builder.Services.AddSyZeroOcelot(o => { /* ... */ }, builder.Configuration);
builder.Services.AddSyZeroGatewayAdmin();

var app = builder.Build();

app.UseSyZero();
app.UseRouting();

app.UseSyZeroGatewayAdmin();

app.MapControllers();
app.MapRazorPages();

// Ocelot is terminal middleware, so branch to avoid swallowing /admin and /admin-api.
app.MapWhen(
    ctx => !ctx.Request.Path.StartsWithSegments("/admin")
           && !ctx.Request.Path.StartsWithSegments("/admin-api"),
    branch => branch.UseSyZeroOcelotAsync().GetAwaiter().GetResult());

await app.RunAsync();
```

## Credentials

> Since `v9.1.0`, the admin panel supports both the legacy single-account settings (`Gateway:Admin:Username` / `Gateway:Admin:Password`) and the newer multi-user settings `Gateway:Admin:Users[]` plus runtime `users.json`. Both forms can coexist; when names collide, the array entry wins. Settings loaded from configuration are treated as built-in accounts (`IsBuiltIn=true`) and cannot be edited in the UI.

Administrator credentials can come from three sources:

1. `Gateway:Admin:Username` + `Gateway:Admin:Password` in `appsettings.json` for a legacy single `Admin` user
2. `Gateway:Admin:Users[]` in `appsettings.json` for multi-user + role configuration
3. `users.json` under content-root, optionally overridden with `Gateway:Admin:UsersFile`

```jsonc
// Legacy single account
{
  "Gateway": { "Admin": { "Username": "admin", "Password": "use-a-strong-password" } }
}
```

```jsonc
// Recommended multi-user configuration
{
  "Gateway": {
    "Admin": {
      "Users": [
        { "Name": "admin", "Password": "use-a-strong-password", "Role": "Admin" },
        { "Name": "viewer", "Password": "another-strong-password", "Role": "Reader" }
      ]
    }
  }
}
```

| Role | Meaning |
|---|---|
| `Admin` | Full access |
| `Reader` | Read-only. Only `GET` is allowed under `/admin-api/*`; write requests return `403`, except logout and self password change |

Authentication supports both **Cookie** auth (via `/admin/login`) and **HTTP Basic** auth for scripts/curl.

### User management

Page: `GET /admin/users`

| Method + path | Description |
|---|---|
| `GET /admin-api/users` | List all users, including `IsBuiltIn`, but never returns passwords |
| `POST /admin-api/users` | Create a file-backed user. Body: `{ "name", "password", "role" }`; duplicate name returns `409` |
| `PUT /admin-api/users/{name}` | Change password / role. Body: `{ "password"?, "role"? }` |
| `DELETE /admin-api/users/{name}` | Delete a file-backed user |
| `POST /admin-api/me/password` | Change the current user's own password |

`users.json` is maintained by `FileGatewayAdminUserStore` using temp-file + atomic replace semantics with in-process `SemaphoreSlim` serialization. It is not a cross-instance sync mechanism; for multi-instance deployments, keep users in configuration or replace the store.

### Login throttling

Since `v9.1.0`, `/admin-api/auth/login` includes per-client-IP in-memory throttling by default: **5 failures / 15-minute window / 5-minute lockout**, returning `429 + Retry-After` once locked.

```json
{
  "Gateway": {
    "Admin": {
      "RateLimit": {
        "MaxFailures": 5,
        "LockoutMinutes": 5,
        "WindowMinutes": 15,
        "CaptchaThreshold": 3,
        "TrustForwardedFor": false,
        "ForwardedHeaderName": "X-Forwarded-For"
      }
    }
  }
}
```

Contract: `IFailedLoginTracker`, with `FailedLoginTracker` as the default singleton implementation.

> When enabling `TrustForwardedFor=true`, make sure the forwarded-IP header is injected only by a trusted reverse proxy and any client-supplied value is stripped first.

#### Distributed tracker via `ICache`

`CachedFailedLoginTracker` is also included in `v9.1.0`. It uses the `ICache` abstraction, typically backed by `SyZero.Redis`, so multiple gateway instances can share failure counters and lockout state:

```csharp
builder.Services
    .AddSyZeroRedis(builder.Configuration)
    .AddSyZeroGatewayAdmin()
    .UseSyZeroGatewayAdminCachedRateLimit();
```

#### Captcha

When `Gateway:Admin:RateLimit:CaptchaThreshold > 0` (default `3`), login requests must also send `CaptchaId` + `CaptchaAnswer` after the threshold is reached.

| Endpoint | Purpose |
|---|---|
| `GET /admin-api/auth/captcha` | Issue a new arithmetic captcha and return `{ id, question }` |

The challenge is stored via `ICaptchaStore`; `MemoryCaptchaStore` is the default implementation with 5-minute TTL and single-use consumption.

### IP allow/deny lists

`AdminIpFilterMiddleware` can block `/admin*` and `/admin-api*` before authentication:

```jsonc
{
  "Gateway": {
    "Admin": {
      "IpFilter": {
        "Allow": [ "10.0.0.0/8", "192.168.0.0/16", "127.0.0.1", "::1" ],
        "Deny":  [ "203.0.113.0/24" ],
        "TrustForwardedFor": false,
        "ForwardedHeaderName": "X-Forwarded-For"
      }
    }
  }
}
```

Rules:

1. `Deny` wins first
2. If `Allow` is non-empty, the request must match it
3. Empty `Allow` + `Deny` means no filtering
4. Both IPv4/IPv6 singles and CIDR ranges are supported
5. IPv4-mapped IPv6 addresses are normalized before matching
6. `IOptionsMonitor` hot-reloads changes without restart

## Pages

| Path | Description |
|---|---|
| `GET /admin` | Overview dashboard with health, counts, provider badge, and divergence warning |
| `GET /admin/routes` | Route CRUD, search, clone, drag-reorder, YAML import/export, markdown descriptions, tester handoff |
| `GET /admin/route-overview` | Aggregated route statistics by `ServiceName`, tags, and methods |
| `GET /admin/swagger-endpoints` | Swagger endpoint CRUD |
| `GET /admin/backups` | Backup list with download / restore / delete |
| `GET /admin/raw-editor` | Raw JSON editor for import / export / full replace |
| `GET /admin/diff` | Line-level diff between stored and runtime configuration |
| `GET /admin/audit` | Audit log viewer |
| `GET /admin/users` | User and role management |
| `GET /admin/rate-limiting` | Global + per-route rate-limit management |
| `GET /admin/load-balancing` | Per-route load-balancer management |
| `GET /admin/tester` | Browser-side route tester with history, favorites, and cURL conversion |
| `GET /admin/login` | Styled login page with "remember me" |
| `GET /admin/logout` | Clears cookie and redirects to login |

## REST APIs

| Method + path | Description |
|---|---|
| `GET /admin-api/health` | Version / config path / latest backup time / stats / `AuditEnabled` |
| `POST /admin-api/reload` | Force reload from disk and apply |
| `GET /admin-api/configuration[?download=1]` | Export full JSON |
| `PUT /admin-api/configuration` | Full replace with automatic backup |
| `GET /admin-api/runtime` | Runtime metadata: `ProviderName` / `StoredHash` / `RuntimeHash` / `Diverged` |
| `GET /admin-api/runtime/diff` | Serialized stored/runtime JSON for UI diffing |
| `GET/POST/PUT/DELETE /admin-api/routes[/{index:int}]` | Route CRUD |
| `POST /admin-api/routes/{index}/move` | `{ "direction": "up"|"down" }` |
| `POST /admin-api/routes/{index}/clone` | Clone route with `-copy` suffix |
| `POST /admin-api/routes/bulk-toggle` | Bulk enable/disable |
| `POST /admin-api/routes/bulk-tags` | Bulk tag operations |
| `GET /admin-api/routes/tags` | All used tags |
| `POST /admin-api/routes/reorder` | Drag-reorder payload |
| `GET /admin-api/routes/export?format=json\|yaml` | Export routes only |
| `POST /admin-api/routes/import?format=json\|yaml&mode=append\|replace` | Import route list |
| `GET/POST/PUT/DELETE /admin-api/swagger-endpoints[/{key}]` | Swagger endpoint CRUD |
| `GET /admin-api/swagger-endpoints/export?format=json\|yaml` | Export Swagger endpoints only |
| `POST /admin-api/swagger-endpoints/import?format=json\|yaml&mode=append\|merge\|replace` | Import Swagger endpoint list |
| `GET /admin-api/services` | Service names from `IServiceManagement` |
| `GET /admin-api/backups` | Backup list |
| `GET /admin-api/backups/{id}/download` | Download one backup |
| `POST /admin-api/backups/{id}/restore` | Restore one backup |
| `DELETE /admin-api/backups/{id}` | Delete one backup |
| `GET /admin-api/audit?take=200&user=&action=&from=&to=` | Read latest audit records with filters |
| `GET /admin-api/audit/export.csv?user=&action=&from=&to=` | Export CSV audit data |
| `GET /admin-api/events` | SSE stream for route / swagger / config / backup write events |
| `GET/PUT/DELETE /admin-api/routes/{index}/rate-limit` | Per-route rate-limit management |
| `GET/PUT/DELETE /admin-api/rate-limit/global` | Global rate-limit management |
| `GET/PUT /admin-api/routes/{index}/load-balancer` | Per-route load-balancer management |
| `GET /admin-api/load-balancer/types` | Supported load-balancer type strings |
| `POST /admin-api/auth/login` | Login and issue `syz_admin_auth` cookie |
| `POST /admin-api/auth/logout` | Logout |
| `GET /admin-api/auth/captcha` | Issue captcha challenge |

> Backup and audit related APIs are optional at the abstraction layer. Implementations that do not support them can return `501 Not Supported`.

## Abstraction extensions

`IGatewayConfigurationStore` also exposes four default interface methods:

```csharp
Task<IReadOnlyList<GatewayConfigurationBackup>> ListBackupsAsync(CancellationToken ct = default);
Task<GatewayConfiguration?> ReadBackupAsync(string id, CancellationToken ct = default);
Task RestoreBackupAsync(string id, CancellationToken ct = default);
Task DeleteBackupAsync(string id, CancellationToken ct = default);
```

Existing implementations do not need to change; unsupported ones can keep the default behavior. `FileGatewayConfigurationStore` provides the complete implementation for backup lifecycle management.

## Backup policy

Before each write, the current `configuration.json` is copied to `configuration.backup.{yyyyMMddHHmmss}.json`. By default, the latest 10 backups are retained.

```json
{
  "Gateway": {
    "Admin": {
      "Backup": { "MaxCount": 30 }
    }
  }
}
```

## Audit log

All write operations (routes / Swagger endpoints / full config replace / backup restore / backup delete) are appended to a JSONL audit file by default:

```json
{
  "Gateway": {
    "Admin": {
      "Audit": {
        "Enabled": true,
        "Path": "logs/gateway-admin-audit.jsonl"
      }
    }
  }
}
```

Default path: `<ContentRoot>/logs/gateway-admin-audit.jsonl`

## Caveats

1. Razor Pages are embedded in the package via `Microsoft.NET.Sdk.Razor`; the host project does not need local `Pages/` files
2. Register `UseSyZeroGatewayAdmin` and `MapControllers` / `MapRazorPages` before Ocelot takes over the pipeline
3. `MMLib.SwaggerForOcelot` only reads `SwaggerEndPoints` at startup, so editing them does not hot-reload the aggregator
4. `System.Text.Json` does not preserve `//` comments when saving `configuration.json`
5. Production deployments should always use HTTPS because Basic Auth over plain HTTP exposes credentials directly

## License

MIT License — see [LICENSE](../../../LICENSE)
