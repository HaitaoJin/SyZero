# Release Notes

This page tracks framework updates for the VitePress site.


## v9.1.0

### Runtime & build

- **TFM unified to `net9.0`**: except for `SyZero.SourceGenerator` (must remain `netstandard2.0` due to Roslyn constraints), all framework modules now target `net9.0`. The previously `netstandard2.1` modules (`SyZero.Core`, `SyZero.Web.Common`, `SyZero.Redis`, `SyZero.SqlSugar`, `SyZero.MongoDB`, `SyZero.EventBus.RabbitMQ`, `SyZero.Logging.Log4Net`, `SyZero.ServiceDiscovery.Consul`, `SyZero.Observability.OpenTelemetry`) and the previously multi-targeted `SyZero.ObjectMapping.AutoMapper` (`net8.0;net9.0`) have all been consolidated to `net9.0`. **Downstream consumers must run on .NET 9 SDK / Runtime**.
- **Solution upgraded to `.slnx`**: `dotnet build SyZero.slnx -c Release` and `dotnet test src/SyZero.Tests/SyZero.Tests.csproj` confirm **273/273 tests passing**.
- **Repository layout reorganized accordingly**: the 18 pluggable module projects formerly under `src/SyZero.Core/SyZero.*` have moved into `src/SyZero.Module/`; the contents of `src/SyZero.Core/SyZero/` were promoted to `src/SyZero.Core/` to match the current modular structure.

### Breaking changes

- **The core NuGet package `SyZero` is now officially renamed to `SyZero.Core`**: replace `dotnet add package SyZero` with `dotnet add package SyZero.Core`. The assembly name `SyZero.dll` and root namespace `SyZero` stay unchanged, so most application code does not need `using` updates.
- **3 lightweight modules physically merged into `SyZero.Core`** — no longer published as standalone NuGet packages, type and API signatures unchanged:

  | Old package / namespace | New home / namespace |
  |---|---|
  | `SyZero.Validation` (`SyZero.Validation.*`) | [`SyZero.Core`](/en/modules/SyZero.Core) ｜ `SyZero.*` |
  | `SyZero.DistributedLock` (`SyZero.DistributedLock.*`) | [`SyZero.Core`](/en/modules/SyZero.Core) ｜ `SyZero.*` |
  | `SyZero.Audit` (`SyZero.Audit.*`) | [`SyZero.Core`](/en/modules/SyZero.Core) ｜ `SyZero.*` |

  - The C# namespaces are merged into `SyZero.*`; `SyZero.Redis.RedisDistributedLockProvider` remains in its own package.
  - **Migration**: drop the corresponding `<PackageReference>` and `using` lines; reflective access to old fully-qualified type names (e.g. `SyZero.Audit.AuditLogEntry`) becomes `SyZero.AuditLogEntry`.

- **7 modules renamed under capability-grouped prefixes** — following the existing `SyZero.Gateway.*` pattern:

  | Old package / namespace | New package / namespace |
  |---|---|
  | `SyZero.RabbitMQ` | [`SyZero.EventBus.RabbitMQ`](/en/modules/SyZero.EventBus.RabbitMQ) |
  | `SyZero.Consul` | [`SyZero.ServiceDiscovery.Consul`](/en/modules/SyZero.ServiceDiscovery.Consul) |
  | `SyZero.Nacos` | [`SyZero.ServiceDiscovery.Nacos`](/en/modules/SyZero.ServiceDiscovery.Nacos) |
  | `SyZero.Log4Net` | [`SyZero.Logging.Log4Net`](/en/modules/SyZero.Logging.Log4Net) |
  | `SyZero.AutoMapper` | [`SyZero.ObjectMapping.AutoMapper`](/en/modules/SyZero.ObjectMapping.AutoMapper) |
  | `SyZero.Mapster` | [`SyZero.ObjectMapping.Mapster`](/en/modules/SyZero.ObjectMapping.Mapster) |
  | `SyZero.OpenTelemetry` | [`SyZero.Observability.OpenTelemetry`](/en/modules/SyZero.Observability.OpenTelemetry) |

  - Package IDs and C# namespaces are renamed in lockstep; no meta-package shims, the old IDs are no longer published. `Add*` / `Use*` extension method signatures are unchanged.

- **Gateway abstractions extracted into [`SyZero.Gateway.Abstractions`](/en/modules/SyZero.Gateway.Abstractions)**: the 11 types previously under `SyZero.Core/Gateway/` (`IGateway`, `IGatewayConfigurationStore`, `IGatewayConfigurationApplier`, `GatewayConfiguration`, `GatewayRoute`, `GatewaySwaggerEndPoint`, `GatewayOptions`, `GatewayLoadBalancerOptions`, `GatewayRateLimitOptions`, `GatewayGlobalRateLimitOptions`, `GatewayConfigurationBackup`) have been pulled out. `SyZero.Ocelot` → [`SyZero.Gateway.Ocelot`](/en/modules/SyZero.Gateway.Ocelot); `SyZero.YARP` → [`SyZero.Gateway.YARP`](/en/modules/SyZero.Gateway.YARP). **The abstractions namespace remains `SyZero.Gateway`** so business `using` directives need no changes; `SyZeroOcelotOptions.CorsPolicyName` / `SyZeroYarpOptions.CorsPolicyName` defaults updated accordingly.

- **Remote service abstractions moved into Core**: protocol-agnostic parts of `SyZero.Feign` now live in `SyZero.Core` under the `SyZero.Client.RemoteService` namespace (`IRemoteServiceProvider`, `IRemoteServiceProxyFactory`, `RemoteServiceOptions`, `RemoteServiceProtocol`, etc.); `SyZero.Feign` is refactored into `FeignRemoteServiceProvider : IRemoteServiceProvider`. Configuration field `Feign.Service[*].DllName` → `AssemblyName`. `AddSyZeroFeign(...)` overload signatures stay compatible (parameter type changes from `FeignOptions` to `RemoteServiceOptions`).

- **Dynamic API provider abstractions moved into Core**: `SyZero.Application.DynamicApi` namespace now provides provider-neutral utilities (`DynamicApiTypeFilter` / `DynamicApiNameNormalizer` / `DynamicApiAttributeCache` / `DynamicApiAssemblyScanner` / `DynamicApiOptionsBase`). Duplicates inside `SyZero.DynamicWebApi` / `SyZero.DynamicGrpc` have been removed in favour of the shared Core implementation; public property shapes are unchanged (no breaking change).

- **Nullable-return tightening**: `IObjectMapper.Map<T>` / `IJsonSerialize.ObjectToJSON` / `JSONToObject<T>` / `ICache.Get<T>` / `IToken.GetPrincipal` / `IXmlSerialize` now declare nullable return values. Callers with `<Nullable>enable</Nullable>` will see compile-time warnings until null checks are added.

### Core enhancements

- **Generic primary key support (backward compatible)**: new `IEntity<TKey>` / `Entity<TKey>` / `IRepository<TEntity, TKey>`. The existing `IEntity` / `Entity` / `IRepository<TEntity>` continue to default to `long` keys via inheritance. EF Core / SqlSugar / MongoDB providers are all generalized to allow `Guid` / `string` / composite keys.
- **Multi-level cache (L1+L2 with cache-penetration / breakdown / avalanche protection)**: new `IMultiLevelCache` + default `MultiLevelCache` (L1 = `IMemoryCache`, L2 = `IDistributedCache`). **Penetration**: `NullSentinel` caches absent results; **breakdown**: `ConcurrentDictionary<key, Lazy<Task>>` SingleFlight; **avalanche**: `JitterRatio` randomizes expirations. `AddSyZeroMultiLevelCache(...)` one-line registration; `SyZero.Redis` automatically provides `RedisDistributedCache` as L2.
- **Configuration change notifier**: new `IConfigurationChangedNotifier` (`event Changed`) backed by `Microsoft.Extensions.Primitives.ChangeToken` over `IConfiguration.GetReloadToken()`. Works with **any configuration source** (appsettings.json / Nacos / Consul / Apollo / etc.).

### Auth & multi-tenancy

- **New module [`SyZero.Authorization`](/en/modules/SyZero.Authorization)**: end-to-end RBAC + JWT Bearer on top of ASP.NET Core Identity. Includes the `User` / `Role` / `PermissionGroup` / `Permission` / `UserRole` / `RolePermission` / `UserPermission` / `RefreshToken` entities; `IPermissionDefinitionProvider` + `PermissionDefinitionManager` + `PermissionSyncHostedService` (syncs definitions to the database on startup); `RbacPermissionChecker` where a user-level explicit deny wins immediately; `JwtTokenManager` (HMAC-SHA256, `ClockSkew=30s`) + `RefreshTokenManager` (DB stores SHA-256 hash only, replay detection cascades a revocation); `SyZeroAccountManager` exposes `LogoutAsync` / `LogoutAllAsync` / `ChangePasswordAsync` / `ResetPasswordAsync`, with automatic global sign-out after a password change. One-line setup: `AddSyZeroAuthorization(builder.Configuration)` + `app.UseSyZeroAuthorization()`.
- **New module [`SyZero.Authorization.AspNetCore`](/en/modules/SyZero.Authorization.AspNetCore)**: `SyPermissionRequirement` + `SyPermissionAuthorizationHandler` bridge `[Authorize(Policy = "syperm:Order.Create")]` to `IPermissionChecker`.
- **New module [`SyZero.Authentication.ApiKey`](/en/modules/SyZero.Authentication.ApiKey)**: minimal API Key auth scheme (request-header allow-list → principal mapping), suitable for service-to-service calls.
- **New module [`SyZero.MultiTenancy`](/en/modules/SyZero.MultiTenancy)**: tenant context + resolution middleware. `ICurrentTenant` (`AsyncLocal<TenantInfo?>`); `HeaderTenantResolver` / `DomainTenantResolver` / `ClaimTenantResolver`; `ITenantStore` abstraction with an `InMemoryTenantStore` demo; `IMultiTenant` entity marker (`long? TenantId`); `ITenantConnectionResolver` for sharded multi-DB extension.
- **New module [`SyZero.MultiTenancy.EntityFrameworkCore`](/en/modules/SyZero.MultiTenancy.EntityFrameworkCore)**: `TenantEntity` + `TenantDbContext` (`SyTenants` table + unique index on `Name`) + `EfCoreTenantStore`.

### Background jobs & distributed transactions

- **New module [`SyZero.BackgroundJobs`](/en/modules/SyZero.BackgroundJobs)**: in-memory background jobs on `System.Threading.Channels`. `IBackgroundJob<TArgs>` + `IBackgroundJobManager.EnqueueAsync<TJob, TArgs>(args)`; `BackgroundJobChannel` defaults to capacity 1024 with optional `DropOldestWhenFull`; `BackgroundJobWorker` supports `WorkerCount` concurrency and isolates job exceptions.
- **New module [`SyZero.BackgroundJobs.Quartz`](/en/modules/SyZero.BackgroundJobs.Quartz)**: Quartz-backed cron scheduling, `AddSyZeroBackgroundJobsQuartz(q => q.AddSyZeroCron<TJob, TArgs>(cron, args))` one-line registration.
- **New module [`SyZero.DistributedTransaction`](/en/modules/SyZero.DistributedTransaction)**: outbox pattern + lightweight saga coordinator. `IOutboxStore` + `OutboxBackgroundService` (configurable `PollIntervalSeconds` / `BatchSize` / `MaxRetryCount`); `ISagaStep<TContext>` + `SagaCoordinator<TContext>` runs steps in order with reverse-order compensation, returning a `SagaResult`.
- **New module [`SyZero.DistributedTransaction.EntityFrameworkCore`](/en/modules/SyZero.DistributedTransaction.EntityFrameworkCore)**: `EfCoreOutboxStore` + `OutboxDbContext` (`SyOutboxMessages` table + `PublishedAtUtc` index).
- **New module [`SyZero.Idempotency`](/en/modules/SyZero.Idempotency)**: `Idempotency-Key` header based middleware, only matches POST / PUT / PATCH / DELETE, replays the original response (StatusCode / ContentType / Body).

### Exception handling & observability

- **Global exception + ProblemDetails (RFC 7807)**: `SyError` + `SyBusinessException` + `SyExceptionMiddleware` emit business exceptions as `application/problem+json` (with `traceId`); uncaught exceptions become `SY-500`. One-line setup with `AddSyZeroExceptionHandler()` + `UseSyZeroExceptionHandler()`.
- **New module [`SyZero.HealthChecks`](/en/modules/SyZero.HealthChecks)**: K8s-friendly three-tier endpoints (`/health/live`, `/health/ready`, `/health`).
- **New module [`SyZero.Diagnostics`](/en/modules/SyZero.Diagnostics)**: request diagnostics middleware that structurally logs method / path / status / duration / TraceId / UserId / TenantId.
- **New module [`SyZero.RateLimiting`](/en/modules/SyZero.RateLimiting)**: wraps the built-in `System.Threading.RateLimiting` + ASP.NET Core middleware; default per-IP `syzero-global` fixed window. Provides `IDistributedRateLimiter` abstraction with a `LocalDistributedRateLimiter` default.
- **New module [`SyZero.DataProtection`](/en/modules/SyZero.DataProtection)**: `[Encrypted("purpose")]` marker + `IFieldEncryptor` abstraction + default `DataProtectorFieldEncryptor` (wraps ASP.NET Core `IDataProtectionProvider`, isolating keys per purpose).

### Resilience, messaging & ecosystem adapters

- **New module [`SyZero.Resilience`](/en/modules/SyZero.Resilience)**: Polly v8 wrapper. `SyResiliencePipelines.Default(...)` provides a default "timeout → retry (exponential backoff + jitter) → circuit breaker (50% failure rate)" pipeline.
- **New module [`SyZero.EventBus.Kafka`](/en/modules/SyZero.EventBus.Kafka)**: Confluent.Kafka-based producer / consumer skeleton. `SyKafkaConsumerBase` is derive-and-go, failed messages flow through `main → *.retry → *.dlq`; producer uses idempotent settings (`EnableIdempotence=true` / `Acks=All`). Retry messages carry a `sy-not-before` header for scheduled retries; back-off is extracted into `SyKafkaBackoff.Compute(retryCount, baseDelay, maxDelay)` (exponential + ±20% jitter + cap).
- **New module [`SyZero.Sdk`](/en/modules/SyZero.Sdk)**: `SyServiceClient` typed-HttpClient base + `AddSyServiceClient<TClient>(baseAddress)` registration; built-in `GetAsync<T>` / `PostAsync<TReq,TResp>` JSON helpers.
- **New module [`SyZero.Realtime`](/en/modules/SyZero.Realtime)**: ASP.NET Core SignalR-backed `SyRealtimeHub` + `ISyRealtimeNotifier` abstraction.
- **New module [`SyZero.Files`](/en/modules/SyZero.Files)**: `ISyFileStorage` abstraction + `LocalFileStorage` default (with built-in path-traversal defense).
- **New module [`SyZero.Mail`](/en/modules/SyZero.Mail)**: `ISyMailSender` abstraction + `SmtpMailSender` + `ConsoleMailSender`.
- **New module [`SyZero.Sms`](/en/modules/SyZero.Sms)**: `ISySmsSender` abstraction + `ConsoleSmsSender` placeholder.
- **New module [`SyZero.FeatureManagement`](/en/modules/SyZero.FeatureManagement)**: wraps `Microsoft.FeatureManagement`, exposes `ISyFeatureManager.IsEnabledAsync(feature)`.
- **New module [`SyZero.Localization`](/en/modules/SyZero.Localization)**: wraps `Microsoft.Extensions.Localization`, exposes `ISyStringLocalizer["key"]`.
- **New module [`SyZero.SourceGenerator`](/en/modules/SyZero.SourceGenerator)**: Roslyn incremental generator skeleton. `SyServiceMarkerGenerator` scans `IApplicationService` types and emits `SyDiscoveredServices.Names`; `SyDIRegistrationGenerator` scans concrete classes implementing `IScopedDependency` / `ISingletonDependency` / `ITransientDependency` and emits `AddSyZeroGenerated(IServiceCollection)` to replace runtime reflection scanning. `netstandard2.0`, consumed as an analyzer.
- **New module [`SyZero.GraphQL`](/en/modules/SyZero.GraphQL)**: HotChocolate wrapper. `AddSyZeroGraphQL(b => ...)` + `MapSyZeroGraphQL("/graphql")`.
- **New module [`SyZero.AI`](/en/modules/SyZero.AI)**: `ISyChatClient` abstraction + `EchoChatClient` placeholder + `MeaiChatClientAdapter` to bridge any `Microsoft.Extensions.AI.IChatClient` (OpenAI / Azure OpenAI / Ollama / etc.) into `ISyChatClient`.

### Gateway sample & Admin panel

- **New sample [`SyZero.Example3.SPA`](/en/guide/example3-spa)**: a single-host sample embedding a React 19 + Vite 6 + Tailwind v4 front end into ASP.NET Core 9, demonstrating `SyZero.AspNetCore.SpaProxy` for dev-time reverse proxy and same-origin packaging on publish.
- **The `SyZero.Gateway` sample now ships with a built-in `/admin` page**: Razor Pages + Tailwind/daisyUI provide CRUD for `Routes` and `SwaggerEndPoints` in `configuration.json`; route changes are hot-applied via `IFileConfigurationSetter` and the file is auto-backed up before each write. Requests return 503 when `Gateway:Admin:Username` / `Gateway:Admin:Password` are not configured.
- **[`SyZero.Gateway.Admin`](/en/modules/SyZero.Gateway.Admin) is published as a standalone module and further expanded**: the admin UI is embedded as a Razor Class Library, enabled with a single `AddSyZeroGatewayAdmin()` call, and depends only on the gateway abstractions so it can pair with different `IGateway` implementations.
- **New module [`SyZero.Gateway.YARP`](/en/modules/SyZero.Gateway.YARP)**: a second `IGateway` backend based on [YARP](https://microsoft.github.io/reverse-proxy/) 2.3, parallel to `SyZero.Gateway.Ocelot` and sharing the same `configuration.json` schema.
- **Unified service-discovery integration**: `SyZeroYarpServiceDiscoveryDestinationResolver` (YARP) and `SyZeroDiscovery` (Ocelot) both build on the same `IServiceManagement` abstraction (Consul / Nacos / Redis / Local / DB). Resolvers refresh affected clusters via `IChangeToken`, no full gateway-config reload required.
- **Cross-backend LB strategy mapping**: `GatewayLoadBalancerOptions.Type` (`RoundRobin` / `LeastConnection` / `NoLoadBalance`) is translated by `YarpConfigMapper` into native YARP policies (`RoundRobin` / `LeastRequests` / `FirstAlphabetical`); the same `configuration.json` can be hot-swapped between backends.
- **`SyZero.Gateway.Admin` greatly enhanced** — full Razor Pages admin panel.
  - **Multi-user + roles (`Admin` / `Reader`)**: legacy single-account config preserved; new `Gateway:Admin:Users[]` adds multi-account + roles. `Reader` can only `GET`, write actions return 403 (with exceptions for logout and self password change). `IGatewayAdminUserStore` + `FileGatewayAdminUserStore` (atomic write, `SemaphoreSlim` serialization); `IsBuiltIn=true` accounts are immutable, the last `Admin` cannot be demoted or deleted.
  - **Per-route rate limiting (Ocelot)**: `GatewayRoute.RateLimitOptions` (`Period` / `PeriodTimespan` / `Limit` / `ClientWhitelist[]`) + `GET/PUT/DELETE /admin-api/routes/{i}/rate-limit`.
  - **Global rate limiting**: `GatewayConfiguration.GetGlobalRateLimit()` / `SetGlobalRateLimit(value?)` bridge to `Extra["GlobalConfiguration"]["RateLimitOptions"]` while preserving sibling fields.
  - **Per-route load balancing**: `GET/PUT /admin-api/routes/{i}/load-balancer`, accepts `RoundRobin` / `LeastConnection` / `NoLoadBalance` only.
  - **Bulk toggle + tags + drag-and-drop reorder**: `Disabled` / `Tags` / `Description` fields; bulk endpoints `bulk-toggle` / `bulk-tags` / `reorder`; YAML import/export (YamlDotNet 16); Markdown rendering for descriptions (marked 12); route overview page (grouped by `ServiceName` with Chart.js 4.4.4 doughnut/bar charts).
  - **Route tester + favorites**: pure browser-side fetch; URL query parameter pre-fill; last 20 calls in localStorage; favorites list with cURL import/export (built-in shell tokenizer).
  - **Backups / raw JSON editor / runtime diff / audit / SSE hot-push**: `configuration.backup.*.json` lifecycle (default `MaxCount=10`); `/admin-api/configuration` whole-config replace; `/admin-api/runtime/diff` line-level highlighting + diff badge; `IGatewayAdminAuditLog` JSONL persistence (`<ContentRoot>/logs/gateway-admin-audit.jsonl`) + filter bar + CSV export; `IGatewayAdminEventBus` channel-based broadcast for write operations, frontend EventSource auto-reconnects.
  - **Login page + rate limiting + captcha + IP allow/deny lists**: glassmorphism `/admin/login` (Cookie + Basic dual protocols); `IFailedLoginTracker` (defaults to per-IP counting, 5 failures → 5-min lockout, returns 429 + Retry-After) + `CachedFailedLoginTracker` (uses `ICache` abstraction for multi-instance sharing); `ICaptchaStore` + `MemoryCaptchaStore` (5-min TTL, single-use) `/admin-api/auth/captcha` arithmetic captcha; `Gateway:Admin:IpFilter` supports IPv4 / IPv6 single-IP and CIDR allow/deny lists (hot-reloaded via `IOptionsMonitor`).
  - **Overall UI polish**: daisyUI multi-theme switcher (10 themes), brand mark + gradient navbar / page header subtitle, card fade-in animations, automatic 401 redirect via `adminFetch`, smoother `adminToast` fade-out.
- **`IGatewayConfigurationStore` gains 4 new default interface methods** (backward compatible): `ListBackupsAsync` / `ReadBackupAsync` / `RestoreBackupAsync` / `DeleteBackupAsync`; implementations that do not support backups return `501 Not Supported`.

### Test coverage

- Test suite grew from 187/187 to 273/273. New coverage includes: multi-level cache / configuration change notifier / idempotency store / outbox / saga / Authorization.AspNetCore / MultiTenancy.EFCore / Quartz / FeatureManagement / Localization / Resilience / Sdk / Realtime / Files / Notifications / Kafka / DistributedTransaction.EFCore / API key auth / Diagnostics / SourceGenerator (including DI registration generator) / GraphQL / AI (including MEAI adapter) / HealthChecks / Audit / RateLimiting / Validation / DataProtection / Gateway.Admin (users + rate limiting + backups + bulk route ops + YAML import/export, 88+ cases).

The authoritative changelog remains in [docs/release-notes.md](../release-notes.md).
