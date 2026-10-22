# 模块总览

SyZero 模块按职责拆分，以下页面已对应仓库中的全部核心模块文档。

## 核心

1. [SyZero.Core](/modules/SyZero.Core)
2. [SyZero.AspNetCore](/modules/SyZero.AspNetCore)
3. [SyZero.Web.Common](/modules/SyZero.Web.Common)
4. [SyZero.Authorization](/modules/SyZero.Authorization)
5. [SyZero.MultiTenancy](/modules/SyZero.MultiTenancy)

## 动态 API

1. [SyZero.DynamicWebApi](/modules/SyZero.DynamicWebApi)
2. [SyZero.DynamicGrpc](/modules/SyZero.DynamicGrpc)
3. [SyZero.Swagger](/modules/SyZero.Swagger)

## 服务治理与通信

1. [SyZero.Gateway.Abstractions](/modules/SyZero.Gateway.Abstractions)
2. [SyZero.Gateway.Ocelot](/modules/SyZero.Gateway.Ocelot)
3. [SyZero.Gateway.YARP](/modules/SyZero.Gateway.YARP)
4. [SyZero.Gateway.Admin](/modules/SyZero.Gateway.Admin)
5. [SyZero.ServiceDiscovery.Consul](/modules/SyZero.ServiceDiscovery.Consul)
6. [SyZero.ServiceDiscovery.Nacos](/modules/SyZero.ServiceDiscovery.Nacos)
7. [SyZero.Feign](/modules/SyZero.Feign)

## 数据与基础设施

1. [SyZero.EntityFrameworkCore](/modules/SyZero.EntityFrameworkCore)
2. [SyZero.SqlSugar](/modules/SyZero.SqlSugar)
3. [SyZero.MongoDB](/modules/SyZero.MongoDB)
4. [SyZero.Redis](/modules/SyZero.Redis)
5. [SyZero.EventBus.RabbitMQ](/modules/SyZero.EventBus.RabbitMQ)
6. [SyZero.DistributedLock](/modules/SyZero.DistributedLock) — 已并入 SyZero.Core (v9.1.0+)

## 工具扩展

1. [SyZero.ObjectMapping.Mapster](/modules/SyZero.ObjectMapping.Mapster) ⭐ 推荐
2. [SyZero.ObjectMapping.AutoMapper](/modules/SyZero.ObjectMapping.AutoMapper)（维护模式）
3. [SyZero.Logging.Log4Net](/modules/SyZero.Logging.Log4Net)
4. [SyZero.Observability.OpenTelemetry](/modules/SyZero.Observability.OpenTelemetry)
5. [SyZero.AspNetCore.SpaProxy](/modules/SyZero.AspNetCore.SpaProxy)
6. [SyZero.BackgroundJobs](/modules/SyZero.BackgroundJobs)

## 可观测、安全与质量（v9.1.0 合并发布）

1. [SyZero.HealthChecks](/modules/SyZero.HealthChecks) — 健康检查（Live/Ready/Overall 三档端点）
2. [SyZero.Audit](/modules/SyZero.Audit) — 审计日志 + 字段脱敏（已并入 SyZero.Core, v9.1.0+）
3. [SyZero.RateLimiting](/modules/SyZero.RateLimiting) — 限流（按 IP / 自定义维度）
4. [SyZero.Validation](/modules/SyZero.Validation) — 校验框架（DataAnnotations 兜底）（已并入 SyZero.Core, v9.1.0+）
5. [SyZero.DataProtection](/modules/SyZero.DataProtection) — 字段级可逆加密
6. [SyZero.Idempotency](/modules/SyZero.Idempotency) — 接口幂等中间件（基于 `Idempotency-Key` 头 + 响应回放）
7. [SyZero.DistributedTransaction](/modules/SyZero.DistributedTransaction) — Outbox 模式 + 简易 Saga 编排器

> 上述能力均已随 `v9.1.0` 正式发布；其中 `SyZero.Core` 新增 **多级缓存（L1+L2 + 防穿透/击穿/雪崩）** 与 **配置热更新通知**，`SyZero.AspNetCore` 新增 **全局异常中间件 + ProblemDetails 响应**。详见对应模块文档。

## 治理与生态适配（v9.1.0 合并发布）

1. [SyZero.Authorization.AspNetCore](/modules/SyZero.Authorization.AspNetCore) — 把 `[Authorize(Policy)]` 桥接到 `IPermissionChecker`
2. [SyZero.MultiTenancy.EntityFrameworkCore](/modules/SyZero.MultiTenancy.EntityFrameworkCore) — 多租户元数据 EF Core 持久化
3. [SyZero.BackgroundJobs.Quartz](/modules/SyZero.BackgroundJobs.Quartz) — 基于 Quartz 的 Cron / 调度后端
4. [SyZero.FeatureManagement](/modules/SyZero.FeatureManagement) — 包装 Microsoft.FeatureManagement
5. [SyZero.Localization](/modules/SyZero.Localization) — 包装 Microsoft.Extensions.Localization

## 运行时与 SDK 适配（v9.1.0 合并发布）

1. [SyZero.Resilience](/modules/SyZero.Resilience) — Polly v8 包装：超时 + 重试 + 熔断的默认管道
2. [SyZero.Sdk](/modules/SyZero.Sdk) — `SyServiceClient` typed-HttpClient 基类
3. [SyZero.Realtime](/modules/SyZero.Realtime) — SignalR 默认实时推送（`ISyRealtimeNotifier`）
4. [SyZero.Files](/modules/SyZero.Files) — 文件存储抽象 + 本地磁盘默认实现
5. [SyZero.Mail](/modules/SyZero.Mail) — 邮件抽象 + SMTP / 控制台默认实现
6. [SyZero.Sms](/modules/SyZero.Sms) — 短信抽象 + 控制台占位实现

## 消息、安全与下一代生态（v9.1.0 合并发布）

1. [SyZero.EventBus.Kafka](/modules/SyZero.EventBus.Kafka) — Kafka 生产 / 消费骨架（主 + 重试 + DLQ 三段式）
2. [SyZero.DistributedTransaction.EntityFrameworkCore](/modules/SyZero.DistributedTransaction.EntityFrameworkCore) — Outbox 的 EF Core 持久化实现
3. [SyZero.Authentication.ApiKey](/modules/SyZero.Authentication.ApiKey) — 最小化 API Key 认证 Scheme
4. [SyZero.Diagnostics](/modules/SyZero.Diagnostics) — 请求诊断中间件（TraceId / UserId / TenantId）
5. [SyZero.SourceGenerator](/modules/SyZero.SourceGenerator) — Roslyn 增量生成器骨架
6. [SyZero.GraphQL](/modules/SyZero.GraphQL) — HotChocolate 包装（默认 `/graphql`）
7. [SyZero.AI](/modules/SyZero.AI) — `ISyChatClient` 抽象 + Echo 占位实现

