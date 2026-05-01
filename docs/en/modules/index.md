# Module Index

All core module docs are available in this section.

## Core

1. [SyZero.Core](/en/modules/SyZero.Core)
2. [SyZero.AspNetCore](/en/modules/SyZero.AspNetCore)
3. [SyZero.Web.Common](/en/modules/SyZero.Web.Common)
4. [SyZero.Authorization](/en/modules/SyZero.Authorization)
5. [SyZero.MultiTenancy](/en/modules/SyZero.MultiTenancy)

## Dynamic API

1. [SyZero.DynamicWebApi](/en/modules/SyZero.DynamicWebApi)
2. [SyZero.DynamicGrpc](/en/modules/SyZero.DynamicGrpc)
3. [SyZero.Swagger](/en/modules/SyZero.Swagger)

## Governance and communication

1. [SyZero.Gateway.Abstractions](/en/modules/SyZero.Gateway.Abstractions)
2. [SyZero.Gateway.Ocelot](/en/modules/SyZero.Gateway.Ocelot)
3. [SyZero.Gateway.YARP](/en/modules/SyZero.Gateway.YARP)
4. [SyZero.Gateway.Admin](/en/modules/SyZero.Gateway.Admin)
5. [SyZero.Consul](/en/modules/SyZero.Consul)
6. [SyZero.Nacos](/en/modules/SyZero.Nacos)
7. [SyZero.Feign](/en/modules/SyZero.Feign)

## Data and infrastructure

1. [SyZero.EntityFrameworkCore](/en/modules/SyZero.EntityFrameworkCore)
2. [SyZero.SqlSugar](/en/modules/SyZero.SqlSugar)
3. [SyZero.MongoDB](/en/modules/SyZero.MongoDB)
4. [SyZero.Redis](/en/modules/SyZero.Redis)
5. [SyZero.RabbitMQ](/en/modules/SyZero.RabbitMQ)
6. [SyZero.DistributedLock](/en/modules/SyZero.DistributedLock)

## Tooling

1. [SyZero.AutoMapper](/en/modules/SyZero.AutoMapper)
2. [SyZero.Log4Net](/en/modules/SyZero.Log4Net)
3. [SyZero.OpenTelemetry](/en/modules/SyZero.OpenTelemetry)
4. [SyZero.AspNetCore.SpaProxy](/en/modules/SyZero.AspNetCore.SpaProxy)
5. [SyZero.BackgroundJobs](/en/modules/SyZero.BackgroundJobs)

## Observability, Security & Quality (new in v1.5.0)

1. [SyZero.HealthChecks](/en/modules/SyZero.HealthChecks) — health checks (live/ready/overall)
2. [SyZero.Audit](/en/modules/SyZero.Audit) — audit logging + field masking
3. [SyZero.RateLimiting](/en/modules/SyZero.RateLimiting) — rate limiting (per-IP / custom partitions)
4. [SyZero.Validation](/en/modules/SyZero.Validation) — validation framework with DataAnnotations fallback
5. [SyZero.DataProtection](/en/modules/SyZero.DataProtection) — field-level reversible encryption
6. [SyZero.Idempotency](/en/modules/SyZero.Idempotency) — API idempotency middleware (`Idempotency-Key` header + response replay)
7. [SyZero.DistributedTransaction](/en/modules/SyZero.DistributedTransaction) — Outbox pattern + simple Saga coordinator

> v1.5.0 also adds **multi-level cache (L1+L2 with cache-penetration / breakdown / avalanche protection)** and **configuration hot-reload notification** to `SyZero.Core`, plus a **global exception middleware with ProblemDetails responses** to `SyZero.AspNetCore`. See the corresponding module pages for details.

## Governance & ecosystem adapters (v1.5.0 follow-up)

1. [SyZero.Authorization.AspNetCore](/en/modules/SyZero.Authorization.AspNetCore) — bridges `[Authorize(Policy)]` to `IPermissionChecker`
2. [SyZero.MultiTenancy.EntityFrameworkCore](/en/modules/SyZero.MultiTenancy.EntityFrameworkCore) — EF Core persistence for tenant metadata
3. [SyZero.BackgroundJobs.Quartz](/en/modules/SyZero.BackgroundJobs.Quartz) — Quartz Cron / scheduling backend
4. [SyZero.FeatureManagement](/en/modules/SyZero.FeatureManagement) — wraps Microsoft.FeatureManagement
5. [SyZero.Localization](/en/modules/SyZero.Localization) — wraps Microsoft.Extensions.Localization

## Runtime & SDK adapters (v1.5.0 batch 3)

1. [SyZero.Resilience](/en/modules/SyZero.Resilience) — Polly v8 wrapper: default timeout + retry + circuit-breaker pipeline
2. [SyZero.Sdk](/en/modules/SyZero.Sdk) — `SyServiceClient` typed-HttpClient base class
3. [SyZero.Realtime](/en/modules/SyZero.Realtime) — SignalR-based default realtime push (`ISyRealtimeNotifier`)
4. [SyZero.Files](/en/modules/SyZero.Files) — file storage abstraction + local-disk default
5. [SyZero.Mail](/en/modules/SyZero.Mail) — mail abstraction + SMTP / console defaults
6. [SyZero.Sms](/en/modules/SyZero.Sms) — SMS abstraction + console placeholder

## Messaging, security & next-gen ecosystem (v1.5.0 batch 4)

1. [SyZero.EventBus.Kafka](/en/modules/SyZero.EventBus.Kafka) — Kafka producer/consumer scaffolding (main + retry + DLQ)
2. [SyZero.DistributedTransaction.EntityFrameworkCore](/en/modules/SyZero.DistributedTransaction.EntityFrameworkCore) — EF Core-backed Outbox store
3. [SyZero.Authentication.ApiKey](/en/modules/SyZero.Authentication.ApiKey) — minimal API Key authentication scheme
4. [SyZero.Diagnostics](/en/modules/SyZero.Diagnostics) — request diagnostics middleware (TraceId / UserId / TenantId)
5. [SyZero.SourceGenerator](/en/modules/SyZero.SourceGenerator) — Roslyn incremental source generator scaffolding
6. [SyZero.GraphQL](/en/modules/SyZero.GraphQL) — HotChocolate wrapper (default `/graphql`)
7. [SyZero.AI](/en/modules/SyZero.AI) — `ISyChatClient` abstraction + Echo placeholder
