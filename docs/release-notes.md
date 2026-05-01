# 发布说明

## v1.6.0

- **网关抽象层独立成包 ｜ 破坏性变更**：
  - 原位于 [src/SyZero.Core/Gateway/](https://github.com/HaitaoJin/SyZero/tree/main/src/SyZero.Core) 下的 11 个类型（`IGateway` / `IGatewayConfigurationStore` / `IGatewayConfigurationApplier` / `GatewayConfiguration` / `GatewayRoute` / `GatewaySwaggerEndPoint` / `GatewayOptions` / `GatewayLoadBalancerOptions` / `GatewayRateLimitOptions` / `GatewayGlobalRateLimitOptions` / `GatewayConfigurationBackup`）**从 `SyZero.Core` 抽出**，仅含接口与 DTO、不引入任何具体网关依赖。
  - 原 `SyZero.Ocelot` 改名为 [`SyZero.Gateway.Ocelot`](/modules/SyZero.Gateway.Ocelot)；原 `SyZero.YARP` 改名为 [`SyZero.Gateway.YARP`](/modules/SyZero.Gateway.YARP)。两者都依赖新包 `SyZero.Gateway.Abstractions` + `SyZero.Core`。
  - C# 命名空间同步调整：`SyZero.Ocelot.*` → `SyZero.Gateway.Ocelot.*`；`SyZero.YARP.*` → `SyZero.Gateway.YARP.*`。**抽象层命名空间保留为 `SyZero.Gateway`**（业务 `using SyZero.Gateway;` 无需修改，仅需将之前依赖 `SyZero.Core` 获取这些类型的项目改为引用 `SyZero.Gateway.Abstractions` 或具体实现包）。
  - `SyZero.Gateway.Admin` 不再从 `SyZero.Core` 获得 `IGateway` / `IGatewayConfigurationStore`，改为引用 `SyZero.Gateway.Abstractions`。
  - `SyZeroOcelotOptions.CorsPolicyName` 默认值 `"SyZero.Ocelot"` → `"SyZero.Gateway.Ocelot"`；`SyZeroYarpOptions.CorsPolicyName` 默认值 `"SyZero.YARP"` → `"SyZero.Gateway.YARP"`。如果业务代码中 `app.UseCors("SyZero.Ocelot")` 或 `app.UseCors("SyZero.YARP")` 直接引用了这些默认名，请同步更新为新名称（或显式设置 `CorsPolicyName` 到原值以保持向后兼容）。
  - **迁移清单**：
    1. 在 csproj 中把 `<PackageReference Include="SyZero.Ocelot" />` 改为 `<PackageReference Include="SyZero.Gateway.Ocelot" />`；YARP 同理。
    2. 业务代码中 `using SyZero.Ocelot;` → `using SyZero.Gateway.Ocelot;`；`using SyZero.YARP;` → `using SyZero.Gateway.YARP;`。
    3. 反射/字符串引用同步替换（如 `Type.GetType("SyZero.Ocelot.OcelotConfigurationApplier")`）。
    4. 如需手工保留旧 CORS 策略名，请在 `AddSyZeroOcelot(...)` / `AddSyZeroYarp(...)` 的选项中显式设置。
  - 全量单测依然通过：**261/261**。

## v1.5.0

- **5 个跨模块新能力（核心稳定性 / 服务治理）**
  - **`SyZero.Core` 多级缓存（L1+L2 + 防穿透/击穿/雪崩）**：新增 `IMultiLevelCache`（`GetOrCreateAsync<T>` / `SetAsync<T>` / `RemoveAsync`）+ `MultiLevelCache` 默认实现（L1 = `IMemoryCache` 进程内缓存，L2 = `IDistributedCache`，可对接任何分布式缓存）。**防穿透**：`NullSentinel` 缓存空值；**防击穿**：`ConcurrentDictionary<key, Lazy<Task>>` 单飞（SingleFlight），同 key 并发只回源一次；**防雪崩**：`JitterRatio` 给过期时间加随机抖动。`AddSyZeroMultiLevelCache(...)` 一行注册；同时给 `SyZero.Redis` 增加了 `RedisDistributedCache` 自动作为 L2。
  - **`SyZero.AspNetCore` 全局异常 + ProblemDetails (RFC 7807)**：`SyError`（`Code` / `Title` / `Detail` / `Type` / `Status`）+ `SyBusinessException` 业务异常；`SyExceptionMiddleware` 把业务异常落为 `application/problem+json` 标准响应（含 `traceId`），未捕获异常落为 `SY-500`；`AddSyZeroExceptionHandler()` + `UseSyZeroExceptionHandler()` 一行接入。
  - **`SyZero.Core` 配置热更新通知**：新增 `IConfigurationChangedNotifier`（`event Changed`）+ `ConfigurationChangedNotifier` 默认实现，基于 `Microsoft.Extensions.Primitives.ChangeToken` 监听 `IConfiguration.GetReloadToken()`，对 **任意配置源**（appsettings.json 文件 / Nacos / Consul / Apollo 等）生效，无需修改对应 Provider。`AppConfig.GetSection<T>` 仍按需读最新配置，无需重新绑定。
  - **新模块 [`SyZero.Idempotency`](/modules/SyZero.Idempotency)**：基于 `Idempotency-Key` 请求头的接口幂等中间件。`IIdempotencyStore`（`TryReserveAsync` / `SaveAsync` / `GetAsync`）+ 默认 `MemoryIdempotencyStore`；首请求占位、缓冲整个响应体并落库；后续相同 key 的请求**直接复用**首次响应（StatusCode / ContentType / Body）。仅命中 POST / PUT / PATCH / DELETE，并发首请求未结束时返回 409；可换 Redis / SQL 实现支持多实例。
  - **新模块 [`SyZero.DistributedTransaction`](/modules/SyZero.DistributedTransaction)**：Outbox 模式 + 简易 Saga 编排器。
    - **Outbox**：`IOutboxStore`（`EnqueueAsync` / `FetchPendingAsync` / `MarkPublishedAsync` / `MarkFailedAsync`）+ 默认 `MemoryOutboxStore`（生产请换库存储）+ `OutboxBackgroundService`（`PollIntervalSeconds` / `BatchSize` / `MaxRetryCount` 可配），定时拉取并通过 `IEventBus.Publish` 投递，失败计数到达上限保留错误等待人工处理。
    - **Saga**：`ISagaStep<TContext>`（`ExecuteAsync` / `CompensateAsync` / `Name`）+ `SagaCoordinator<TContext>`，顺序执行；任一步抛异常时按已执行顺序的**反向**调用 `CompensateAsync`，结果以 `SagaResult { IsSuccess, Error?, CompensationErrors[] }` 返回。
  - **测试覆盖**：新增 `MultiLevelCacheTests` / `ConfigurationChangedNotifierTests` / `IdempotencyStoreTests` / `OutboxStoreTests` / `SagaCoordinatorTests` 共 9 个用例；全套从 237/237 升至 **246/246 通过**。

- **5 个治理与生态适配新模块（v1.5.0 续集）**
  - **[`SyZero.Authorization.AspNetCore`](/modules/SyZero.Authorization.AspNetCore)**：`SyPermissionRequirement` + `SyPermissionAuthorizationHandler` 把 ASP.NET Core 授权策略 `[Authorize(Policy = "syperm:Order.Create")]` 桥接到 `IPermissionChecker`，与 `SyZero.Authorization` 的 RBAC 实现自然衔接，未授权返回 403。`AddSyZeroAuthorizationBridge()` + `AddSyPermission(...)` 一行接入。
  - **[`SyZero.MultiTenancy.EntityFrameworkCore`](/modules/SyZero.MultiTenancy.EntityFrameworkCore)**：`TenantEntity` + `TenantDbContext`（`SyTenants` 表 + `Name` 唯一索引）+ `EfCoreTenantStore` 默认替换 `InMemoryTenantStore`，`AddSyZeroMultiTenancyEfCore(opt => opt.UseSqlite(...))` 一行接入；业务可继承扩展或合入主 DbContext。
  - **[`SyZero.BackgroundJobs.Quartz`](/modules/SyZero.BackgroundJobs.Quartz)**：基于 Quartz 的 Cron 调度后端，复用 `IBackgroundJob<TArgs>` 抽象。`SyZeroQuartzJobAdapter` 通过 `JobDataMap` 还原任务类型 + JSON 参数，`AddSyZeroBackgroundJobsQuartz(q => q.AddSyZeroCron<TJob, TArgs>(cron, args))` 一行注册。
  - **[`SyZero.FeatureManagement`](/modules/SyZero.FeatureManagement)**：包装 `Microsoft.FeatureManagement`，提供 `ISyFeatureManager.IsEnabledAsync(feature)` 抽象，业务直接依赖此接口而非 `IFeatureManager`，便于 mock 与替换。`AddSyZeroFeatureManagement(builder.Configuration)` 默认从 `FeatureManagement` 节读开关。
  - **[`SyZero.Localization`](/modules/SyZero.Localization)**：包装 `Microsoft.Extensions.Localization`，提供 `ISyStringLocalizer["key"]` / `["key", args]` 抽象。`AddSyZeroLocalization("Resources")` 一行接入；缺失资源时自动回退到 key 本身。
  - **测试覆盖**：新增 `AuthorizationAspNetCoreTests` / `MultiTenancyEfCoreTests` / `QuartzAdapterTests` / `FeatureManagementTests` / `LocalizationTests` 共 9 个用例；全套从 246/246 升至 **255/255 通过**。

- **6 个运行时与 SDK 适配新模块（v1.5.0 第三批）**
  - **[`SyZero.Resilience`](/modules/SyZero.Resilience)**：Polly v8 包装。`SyResiliencePipelines.Default(...)` 一行获取「超时 → 重试（指数退避 + 抖动）→ 熔断（50% 失败率）」默认管道；`AddSyZeroResilience()` 注册名为 `"default"` 的命名管道，业务通过 `ResiliencePipelineProvider<string>.GetPipeline(...)` 拿取。
  - **[`SyZero.Sdk`](/modules/SyZero.Sdk)**：`SyServiceClient` typed-HttpClient 基类 + `AddSyServiceClient<TClient>(baseAddress)` 一行注册；自带 `GetAsync<T>` / `PostAsync<TReq,TResp>` JSON 快捷方法，便于跨服务客户端 SDK 编写，可与 `Microsoft.Extensions.Http.Resilience` 组合。
  - **[`SyZero.Realtime`](/modules/SyZero.Realtime)**：基于 ASP.NET Core SignalR 的 `SyRealtimeHub` + `ISyRealtimeNotifier` 抽象。`AddSyZeroRealtime()` + `app.MapSyZeroRealtime("/hubs/sy-realtime")` 一行接入；业务侧不直接依赖 `IHubContext` 便于单测与替换。
  - **[`SyZero.Files`](/modules/SyZero.Files)**：`ISyFileStorage` 抽象 + `LocalFileStorage` 本地磁盘默认实现（内置目录穿越防御）。`AddSyZeroLocalFiles(rootPath)` 一行注册；后续可接 S3 / OSS / COS。
  - **[`SyZero.Mail`](/modules/SyZero.Mail)**：`ISyMailSender` 抽象 + `SmtpMailSender`（基于 `System.Net.Mail.SmtpClient`）+ `ConsoleMailSender` 占位。`AddSyZeroSmtpMail(o => ...)` / `AddSyZeroConsoleMail()` 双模式注册。
  - **[`SyZero.Sms`](/modules/SyZero.Sms)**：`ISySmsSender` 抽象 + `ConsoleSmsSender` 占位实现，便于以派生包形式接入阿里云、腾讯云、Twilio 等具体通道。
  - **测试覆盖**：新增 `ResilienceTests` / `SdkTests` / `RealtimeTests` / `FilesTests` / `NotificationsTests` 共 6 个用例；全套从 255/255 升至 **261/261 通过**。

- **7 个消息、安全与下一代生态新模块（v1.5.0 第四批）**
  - **[`SyZero.EventBus.Kafka`](/modules/SyZero.EventBus.Kafka)**：基于 Confluent.Kafka 的生产者 / 消费者骨架。`SyKafkaConsumerBase` 派生即用，处理失败按「主 → `*.retry` → `*.dlq`」三段式自动转发；生产者启用幂等（`EnableIdempotence=true` / `Acks=All`）。
  - **[`SyZero.DistributedTransaction.EntityFrameworkCore`](/modules/SyZero.DistributedTransaction.EntityFrameworkCore)**：`EfCoreOutboxStore` + `OutboxDbContext`（`SyOutboxMessages` 表 + `PublishedAtUtc` 索引），通过 `services.Replace(...)` 替换默认 `MemoryOutboxStore`。
  - **[`SyZero.Authentication.ApiKey`](/modules/SyZero.Authentication.ApiKey)**：最小化 API Key 认证 Scheme（请求头白名单 → 主体名映射），`builder.AddSyZeroApiKey(o => o.Keys[...] = "...")` 一行接入，适合服务间 / 受控集成。
  - **[`SyZero.Diagnostics`](/modules/SyZero.Diagnostics)**：请求诊断中间件，结构化日志记录 method / path / status / 耗时 / TraceId / UserId / TenantId，并写入 `HttpContext.Items` 便于业务读取。
  - **[`SyZero.SourceGenerator`](/modules/SyZero.SourceGenerator)**：Roslyn 增量生成器骨架，示例 `SyServiceMarkerGenerator` 扫描 `IApplicationService` 类型并生成 `SyDiscoveredServices.Names`，便于后续生成 DI 注册 / Controller 适配。`netstandard2.0`，以 analyzer 形式被消费。
  - **[`SyZero.GraphQL`](/modules/SyZero.GraphQL)**：HotChocolate 包装。`AddSyZeroGraphQL(b => ...)` + `MapSyZeroGraphQL("/graphql")` 一行接入，默认 `SyZeroPingQuery` 暴露 `ping: String` 占位字段。
  - **[`SyZero.AI`](/modules/SyZero.AI)**：`ISyChatClient` 抽象 + `EchoChatClient` 占位实现；具体大模型适配（OpenAI、Azure OpenAI、Ollama 等）通过派生 `ISyChatClient` 注入即可。仅依赖 `Microsoft.Extensions.AI` 抽象包，不绑定具体 SDK。
  - **测试覆盖**：新增 `KafkaEventBusTests` / `DistributedTransactionEfCoreTests` / `ApiKeyAuthenticationTests`（3 个）/ `DiagnosticsMiddlewareTests` / `SourceGeneratorTests` / `GraphQLTests` / `AIChatClientTests` 共 9 个用例；全套从 261/261 升至 **270/270 通过**。

- **第四批增量升级（v1.5.0 制造供应链、决策、生态接入）**
  - **`SyZero.SourceGenerator` 新增 `SyDIRegistrationGenerator`**：扫描实现 `IScopedDependency` / `ISingletonDependency` / `ITransientDependency` 的非抽象类，生成 `SyZero.SourceGenerator.Generated.SyZeroGeneratedRegistration.AddSyZeroGenerated(IServiceCollection)` 扩展方法。如果类型同时实现其他业务接口，会以 `services.AddXxx<TInterface>(sp => sp.GetRequiredService<TImpl>())` 同时注册接口映射，可以替代传统的运行时反射扫描。
  - **`SyZero.EventBus.Kafka` 增加 scheduled retry**：重试消息携带 `sy-not-before` （Unix ms）header，消费到未到期会 `Task.Delay` 至到期再 Handle；退避策略抽出 `SyKafkaBackoff.Compute(retryCount, baseDelay, maxDelay)`（指数退避 + ±20% 抖动 + 上限封顶），可独立单测。业务可通过重写 `RetryBaseDelay` / `RetryMaxDelay` / `MaxRetry` 参数调优。
  - **`SyZero.AI` 接入 `Microsoft.Extensions.AI.IChatClient`**：新增 `MeaiChatClientAdapter` 把任意 `IChatClient`（OpenAI / Azure OpenAI / Ollama / 自建网关）适配为 `ISyChatClient`；提供 `AddSyZeroAIChatClient(IChatClient)` / `AddSyZeroAIChatClient(Func<IServiceProvider, IChatClient>)` 两种注入方式。业务代码仍然只依赖 `ISyChatClient`，便于 mock 与替换。
  - **测试覆盖**：新增 `DIRegistrationGenerator_EmitsLifetimeAwareRegistrations` / `Backoff_ExponentialWithJitter_AndCapped` / `MeaiAdapter_BridgesAnyIChatClient` 共 3 个用例；全套从 270/270 升至 **273/273 通过**。

- **5 个新模块（可观测、安全与质量）**
  - **[`SyZero.HealthChecks`](/modules/SyZero.HealthChecks)**：在 `Microsoft.Extensions.Diagnostics.HealthChecks` 之上提供 SyZero 风格注册与 K8s 友好的三档端点（`/health/live`、`/health/ready`、`/health`）。可通过 `LiveTag` / `ReadyTag` 区分检查器，默认输出每个检查项的状态、耗时、异常的 JSON 详情。`AddSyZeroHealthChecks(...)` 返回 `IHealthChecksBuilder` 供链式注册具体检查器，`endpoints.UseSyZeroHealthChecks()` 一行映射全部端点。
  - **[`SyZero.Audit`](/modules/SyZero.Audit)**：`AuditLogEntry`（Action / Resource / Duration / Success / Extras）+ `IAuditLogStore`（默认 `MemoryAuditLogStore`，可替换为 EF / Mongo / ES）+ `IAuditDataMasker`（基于 `[Sensitive]` 反射脱敏，序列化为 JSON 时把敏感属性替换为 `***`）。`netstandard2.1`，可在任意分层使用。
  - **[`SyZero.RateLimiting`](/modules/SyZero.RateLimiting)**：包装 .NET 内置 `System.Threading.RateLimiting` + ASP.NET Core 中间件，默认按客户端 IP 注册 `syzero-global` 固定窗口策略；同时提供 `IDistributedRateLimiter` 抽象与进程内默认实现 `LocalDistributedRateLimiter`，分布式实现可由 `SyZero.Redis` 提供。
  - **[`SyZero.Validation`](/modules/SyZero.Validation)**：`ISyValidator<T>` + `SyValidationResult` 抽象 + `ISyValidatorRunner` 入口 + `DataAnnotationsValidator` 兜底实现（基于 `System.ComponentModel.DataAnnotations`）。业务可注册任意 `ISyValidator<T>` 适配 FluentValidation 等库，`netstandard2.1`，零三方依赖。
  - **[`SyZero.DataProtection`](/modules/SyZero.DataProtection)**：`[Encrypted("purpose")]` 标记 + `IFieldEncryptor` 抽象 + 默认 `DataProtectorFieldEncryptor`（包装 ASP.NET Core `IDataProtectionProvider`，不同 Purpose 隔离密钥）。`AddSyZeroDataProtection(configure?)` 可透传 `PersistKeysToFileSystem` / Redis / Azure Key Vault 等密钥环配置。
  - **测试覆盖**：`src/SyZero.Tests/{HealthChecksTests,AuditTests,RateLimitingTests,ValidationTests,DataProtectionTests}.cs` 新增 11 个用例，全套 237/237 通过。
- **`SyZero.Gateway.Admin` — 多用户 + 角色 + 路由限流 + 负载均衡管理（兼容旧版凭据）**
  - **凭据兼容**：保留旧版单账号 `Gateway:Admin:Username` / `Gateway:Admin:Password`（默认角色 `Admin`），同时**新增** `Gateway:Admin:Users[]` 多账号 + 角色（`Name` / `Password` / `Role`）；两种 appsettings 写法可并存（同名时数组项覆盖单账号项）。启动时若仍未解析到任何账号才抛 `InvalidOperationException` 并提示两种迁移示例。
  - **角色（`Admin` / `Reader`）**：`Reader` 仅能 `GET /admin-api/*`，写操作返回 `403`；例外允许 `POST /admin-api/auth/logout` 与 `POST /admin-api/me/password`（改自己的密码）。中间件根据角色注入 `ClaimTypes.Role` 与自定义 `gw:builtin` Claim。
  - **运行时账号管理**：新增 `IGatewayAdminUserStore` + 默认 `FileGatewayAdminUserStore`（`<ContentRoot>/users.json`，可由 `Gateway:Admin:UsersFile` 覆盖；写临时文件 + 原子 replace + `SemaphoreSlim` 串行化）；`IUserResolver` 合并 appsettings + 文件，appsettings 优先并标记 `IsBuiltIn=true`。
  - **REST + 页面**：`GET /admin-api/users`、`POST /admin-api/users`、`PUT /admin-api/users/{name}`、`DELETE /admin-api/users/{name}`、`POST /admin-api/me/password`；`GET /admin/users` 提供新增 / 编辑 / 删除 / 改自己密码的 UI。`IsBuiltIn=true` 不可改不可删，最后一个 Admin 不可降级 / 删除，禁止删除当前登录用户。
  - **路由限流（Ocelot）**：`GatewayRoute` 新增 `RateLimitOptions` 字段（`EnableRateLimiting` / `Period`（`1s`/`5m`/`1h`/`1d`）/ `PeriodTimespan` / `Limit` / `ClientWhitelist[]`）；新增 `GET/PUT/DELETE /admin-api/routes/{i}/rate-limit` 三个端点，`Period` 正则与 `Limit > 0` 在服务端做强校验。
  - **全局限流**：`GatewayConfiguration` 新增 `GetGlobalRateLimit()` / `SetGlobalRateLimit(value?)` 桥接 `Extra["GlobalConfiguration"]["RateLimitOptions"]`，保留同节点的其它字段（如 `BaseUrl`）；新增 `GET/PUT/DELETE /admin-api/rate-limit/global`，`HttpStatusCode` 限制 100–599。
  - **路由负载均衡**：新增 `GET/PUT /admin-api/routes/{i}/load-balancer` 与 `GET /admin-api/load-balancer/types`，仅接受 `RoundRobin` / `LeastConnection` / `NoLoadBalance`。
  - **页面**：`GET /admin/rate-limiting`（全局卡片 + 路由表 + 编辑对话框）、`GET /admin/load-balancing`（路由表 + 内联下拉一键应用）。`_AdminLayout` 侧栏 / navbar 新增对应入口与角色徽章 + 用户管理菜单。
  - **测试**：`src/SyZero.Tests/GatewayAdminUsersAndRateLimitTests.cs` 新增 24 个用例，覆盖角色规范化、文件存储 CRUD、UserResolver 合并、中间件 401/403/200、UsersController 业务规则（重名 / 内置 / 最后一个 Admin / 自删保护 / 改自己密码）、`GatewayConfiguration` 全局限流桥（含其它字段保留 / null 清除）、路由限流端点（Period 正则 / Limit > 0 / NotFound）、负载均衡端点（类型校验）。`GatewayAdminTests`+`GatewayAdminUsersAndRateLimitTests` 合计 88/88 通过。

## v1.4.0

- **新模块 [`SyZero.MultiTenancy`](/modules/SyZero.MultiTenancy)**：多租户上下文 + 解析中间件。
  - **`ICurrentTenant`**：基于 `AsyncLocal<TenantInfo?>` 的当前租户上下文（Scoped 注册）；`Change(tenant)` 返回 `IDisposable` 便于后台任务手工切上下文，跨 `await` 自动流转。
  - **`ITenantResolver`**：内置 `HeaderTenantResolver`（默认 `__tenant` 头）/ `DomainTenantResolver`（按 `DomainSuffix` 取子域）/ `ClaimTenantResolver`（取 `tenantid` / `tenant` Claim）三种实现，按注册顺序依次尝试，命中即停。
  - **`ITenantStore`**：抽象 `FindByIdAsync` / `FindByNameAsync`；内置 `InMemoryTenantStore` 用于演示与单元测试。业务侧实现自己的 `ITenantStore` 即可对接 EF Core / SqlSugar / MongoDB。
  - **`MultiTenancyMiddleware`**：跑全部 Resolver → 调 `ITenantStore` → 写入 `ICurrentTenant.Change(tenant)`，请求结束自动还原；只接受 `IsActive=true` 的租户。
  - **`IMultiTenant` 实体标记**：`long? TenantId`，业务侧在 `DbContext.OnModelCreating` 配合 `ICurrentTenant` 加 `HasQueryFilter` 实现单库过滤。
  - **`ITenantConnectionResolver`**：分库 / 多 DB 扩展点，默认实现读 `TenantInfo.ConnectionString`，业务可替换为按 Id 路由到 N 个分库或 Sharding 中心。
  - **一行接入**：`builder.Services.AddSyZeroMultiTenancy(builder.Configuration)` + `app.UseSyZeroMultiTenancy()`，配置走 `MultiTenancy` 节。
- **新模块 [`SyZero.BackgroundJobs`](/modules/SyZero.BackgroundJobs)**：基于 `System.Threading.Channels` 的内存后台任务。
  - **`IBackgroundJob<TArgs>`** 任务契约 + **`IBackgroundJobManager.EnqueueAsync<TJob, TArgs>(args)`** 投递入口（Singleton），每次执行新建 `IServiceScope`、Job 默认 Scoped。
  - **`BackgroundJobChannel`**：`BoundedChannel`，默认容量 1024 + 满时等待；可配 `DropOldestWhenFull=true` 改为「丢弃最老」无阻塞策略；`QueueCapacity=0` 退化为无界队列（仅本地开发）。
  - **`BackgroundJobWorker : BackgroundService`**：可配 `WorkerCount` 并发度（默认 1），任务异常自动 `ILogger.LogError` 后继续消费，**不影响其他任务**；`OperationCanceledException` 透传以响应优雅关停。
  - **一行接入**：`builder.Services.AddSyZeroBackgroundJobs(builder.Configuration, opt => ...)` + `services.AddBackgroundJob<TJob>()` 注册每种任务，配置走 `BackgroundJobs` 节。
  - **未提供**：自动重试 / 持久化 / 延迟 / Cron 调度（计划在 `SyZero.BackgroundJobs.Hangfire` / `Quartz` 子模块中提供）。
- **新模块 [`SyZero.DistributedLock`](/modules/SyZero.DistributedLock)**：分布式锁抽象 + 进程内默认实现 + Redis 后端。
  - **`IDistributedLockProvider.AcquireAsync(key, expiration, waitTimeout, ct)`** 返回 `IAsyncDisposable?`：成功返回锁句柄，超时 / 取消返回 `null`；`waitTimeout=Zero` 表示「不等待，仅尝试一次」。
  - **`LocalDistributedLockProvider`**（在 `SyZero.DistributedLock` 包内）：基于 `SemaphoreSlim` + 过期 `CancellationToken` 自动释放。**仅适合单实例 / 测试**。
  - **`RedisDistributedLockProvider`**（位于 `SyZero.Redis` 包内）：`SET NX PX` 加锁 + Lua 脚本校验 token 后释放（**不会误删别人的锁**）；引入 `SyZero.Redis` 时**自动注册为默认 `IDistributedLockProvider`**，业务无需感知后端切换。
  - **释放幂等**：`DisposeAsync` 多次调用安全；过期时间是死锁兜底，即使忘记 `await using` 也能被自动回收。
- **测试覆盖**：`src/SyZero.Tests/MultiTenancyTests.cs` / `BackgroundJobsTests.cs` / `DistributedLockTests.cs` 新增 6 个用例，全套 187/187 通过。

## v1.3.0

- **新模块 [`SyZero.Authorization`](/modules/SyZero.Authorization)**：基于 ASP.NET Core Identity 的 RBAC + JWT Bearer 认证授权一站式实现。
  - **实体**：`User` / `Role` / `PermissionGroup` / `Permission` / `UserRole` / `RolePermission` / `UserPermission` / `RefreshToken`，全部 `: Entity`（`long` 主键）。
  - **权限定义聚合**：`IPermissionDefinitionProvider` + `PermissionDefinitionManager`（`ISingletonDependency`，懒加载聚合所有 Provider），支持分组 / 父子树 / 排序 / 启停；新增 `PermissionSyncHostedService` 在应用启动时把代码侧定义同步落库（按 `Name` Upsert）。
  - **`IPermissionChecker` 默认实现 `RbacPermissionChecker`**：`IsHas(name)` 同步判定 —— 用户级直接覆盖（`UserPermission.IsGranted`）优先于角色聚合，**任一显式 deny 立即生效**。注册即覆盖 `SyZero.Core` 内的占位实现。
  - **JWT**：`JwtTokenManager : IToken` 提供 `CreateToken / GetPrincipal`（HMAC-SHA256，`ClockSkew=30s`，失败返回 `null`）；`JwtOptions` 走 `Configuration.GetSection("Jwt")` + `Action<JwtOptions>` 双通道。
  - **RefreshToken（脱机刷新令牌）**：`IRefreshTokenManager` + `RefreshTokenManager`，32 字节 `RandomNumberGenerator` + Base64Url，**数据库只存 SHA-256 hash**；`RotateAsync` 检测重放（已撤销令牌再用 → 级联 `RevokeAllAsync` 并抛 `SyMessageException`）；`RevokeAllAsync(userId)` 用于「全终端登出」。
  - **`SyZeroAccountManager`**（`IScopedDependency`）：粘合 `UserManager<User>` + `IRefreshTokenManager`，提供 `LogoutAsync` / `LogoutAllAsync` / `ChangePasswordAsync` / `ResetPasswordAsync` —— 改密 / 重置密码成功后自动 `RevokeAllAsync` 强制全终端下线。
  - **Identity Stores**：`SyZeroUserStore`（`IUserPasswordStore` + `IUserEmailStore` + `IUserSecurityStampStore` + `IUserPhoneNumberStore` + `IUserLockoutStore` + `IUserTwoFactorStore`）与 `SyZeroRoleStore`，全部基于 `IRepository<TEntity, long>`。
  - **一行接入**：`builder.Services.AddSyZeroAuthorization(builder.Configuration)` + `app.UseSyZeroAuthorization()`，内部自动 `AddIdentityCore<User>().AddRoles<Role>().AddDefaultTokenProviders()` + `AddAuthentication(JwtBearer).AddJwtBearer(...)` + `AddAuthorization()`。
  - 新增 NuGet 依赖：`Microsoft.AspNetCore.Authentication.JwtBearer 9.0.0`（已加入 [Directory.Packages.props](/) 中央版本管理）。
- **`SyZero.Core` 新增泛型主键支持（向后兼容）**：
  - 新增 `IEntity<TKey>` / `Entity<TKey>` 与 `IRepository<TEntity, TKey>`；现有 `IEntity` / `Entity` / `IRepository<TEntity>` 通过继承保留默认 `long` 主键，**所有现有代码无需修改**。
  - `SyZero.EntityFrameworkCore` / `SyZero.SqlSugar` / `SyZero.MongoDB` 三大 Provider 同步泛化，可定义 `Guid` / `string` / 复合 ID 等任意主键类型的实体仓储。
- **可空返回值收紧（轻度破坏性）**：`IObjectMapper.Map<T>` / `IJsonSerialize.ObjectToJSON` / `JSONToObject<T>` / `ICache.Get<T>` / `IToken.GetPrincipal` / `IXmlSerialize` 一组接口的返回值改为可空，旧调用方启用 `<Nullable>enable</Nullable>` 后会得到编译期警告，请按需补 null 检查。

## v1.2.3

- **`SyZero.Gateway.Admin` 大幅增强**：
  - 新增「备份」页：列出 / 下载 / 还原 / 删除 `configuration.backup.*.json`，对应 `GET/POST/DELETE /admin-api/backups[/{id}]`；保留份数可由 `Gateway:Admin:Backup:MaxCount`（默认 10）调整。
  - 新增「原生 JSON 编辑器」页：导入 / 导出 / 客户端 JSON 校验 / 全量替换，对应 `GET/PUT /admin-api/configuration`（`?download=1` 触发文件下载）。
  - 新增「运行时 Diff」页：对比磁盘配置与 `IGateway.GetConfigurationAsync()` 返回的运行时配置，行级高亮，对应 `GET /admin-api/runtime` 与 `GET /admin-api/runtime/diff`，并在概览页用横幅提示「Diverged」。
  - 新增「审计日志」页：JSONL 落盘（默认 `<ContentRoot>/logs/gateway-admin-audit.jsonl`），可由 `Gateway:Admin:Audit:Enabled / Path` 控制；对应 `GET /admin-api/audit`，新增 `IGatewayAdminAuditLog` 抽象 + 默认 `FileGatewayAdminAuditLog`。
  - 新增「路由测试器」页：纯浏览器侧 fetch（不经过 `/admin-api`），即时显示状态 / 耗时 / 响应头 / 响应体（JSON 自动美化）。
  - 路由 UX 升级：搜索过滤、上 / 下移、克隆、`ServiceName` 自动补全（来自 `IServiceManagement`），新增 `POST /admin-api/routes/{i}/move` 与 `POST /admin-api/routes/{i}/clone` 与 `GET /admin-api/services`。
  - **路由批量启停 + 标签分组**：`GatewayRoute` 新增 `Disabled`（bool）与 `Tags`（List<string>）字段；Admin 面板支持多选 → 批量启用/禁用/打标签，按标签/状态过滤。`SyZero.Gateway.Ocelot` 与 `SyZero.Gateway.YARP` 在下发底层配置时**跳过 `Disabled=true` 的路由**，但仍保留在 `configuration.json` 中以便编辑。新增 `POST /admin-api/routes/bulk-toggle` / `POST /admin-api/routes/bulk-tags` / `GET /admin-api/routes/tags`。
  - **拖拽排序 + Markdown 描述 + YAML 导入导出**：Routes 页接入 [SortableJS](https://sortablejs.github.io/Sortable/) CDN 实现拖拽重排，后端新增 `POST /admin-api/routes/reorder`；`GatewayRoute` 新增 `Description` 元数据字段（不下发到后端，在表格中以 tooltip 呈现）；新增基于 [YamlDotNet 16.1.3](https://github.com/aaubry/YamlDotNet) 的 `GET /admin-api/routes/export?format=yaml\|json` 与 `POST /admin-api/routes/import?format=&mode=append\|replace`。
  - **路由描述 Markdown 渲染 + 路由概览面板**：Routes 页使用 [marked 12](https://marked.js.org/) CDN 在描述对话框中渲染 Markdown；新增 `/admin/route-overview` 页面，按 `ServiceName` 分组统计总数/启用/禁用/方法分布/标签，并提供按标签/按 HTTP Method 的计数面板。页面集成 [Chart.js 4.4.4](https://www.chartjs.org/) CDN，渲染「启用 vs 禁用」环图、「ServiceName Top 10」与「HTTP Method」的柱状图。概览页表格与标签徒为可点击链接，跳转 `/admin/routes?service=&tag=&status=` 并预填对应过滤。
  - **路由测试器增强**：Tester 页读取 URL 查询参数 `?method=&url=&headers=&body=` 预填表单；Routes 页操作列新增「▶ 发送到测试器」，以路由的 `UpstreamPathTemplate` + 首个合法 HTTP Method 新开页签打开测试器；所有请求（含失败）自动以 localStorage 保留最近 20 条，可一键「回填」或删除。
  - **Routes 页「列显示设置」与「表头点击排序」**：工具栏新增「列 ▾」下拉，可勾选隐藏 `#`/状态/Upstream/Methods/Downstream/Service/Tags/SwaggerKey 任意列（隐藏状态存 localStorage `gw-routes-cols-hidden`）；表头点击可依次在 `↑ → ↓ → 不排序` 三状态忪环（存 `gw-routes-sort`），客户端排序不会修改后端真实顺序；客户端排序激活时拖拽重排被自动禁用以防错乱。
  - **Tester 收藏夹与 cURL 互转**：Tester 页新增「收藏夹」卡片（按可选分组列表、一键「回填」/「转 cURL」/删除，存 localStorage `gw-tester-favorites`）；提供「复制 cURL」「从 cURL 导入」「导出收藏」「导入收藏」四个按钮；内置轻量级 shell 词法解析器（支持单/双引号、反斜杠转义、续行 `\` 合并），并将 `http(s)://host/path?query` 自动限制为 path+query（管理页同域调用语义）。
  - **xUnit 测试覆盖**：`src/SyZero.Tests/GatewayAdminTests.cs` 新增 13 个用例，覆盖 `FileGatewayConfigurationStore`（备份生命周期 / `MaxCount` 截断 / 非法 backupId 拒绝）、`AdminController` 端点（`reorder` 排列校验 / `bulk-toggle` / `bulk-tags` set·add·remove / `import·export` JSON+YAML 互转、`replace` vs `append` 模式、按首字符自动检测格式）以及 `SyZero.Gateway.Ocelot` / `SyZero.Gateway.YARP` 映射时跳过 `Disabled=true` 路由的契约。
  - **Swagger 端点批量导入/导出**：参照 Routes 端点形态新增 `GET /admin-api/swagger-endpoints/export?format=json\|yaml` 与 `POST /admin-api/swagger-endpoints/import?format=&mode=append\|merge\|replace`；`append` 拒绝 Key 冲突，`merge` 按 Key 覆盖/新增，`replace` 全量覆盖；缺失 Key 的条目会被拒绝。Swagger 端点页新增「⬇ 导出 JSON / YAML」与「⬆ 导入」按钮（含文件选择 + 文本粘贴 + 格式自动按文件名识别），测试覆盖扩展至 17 个用例。
  - **概览页自动刷新与 diff badge**：概览页新增「自动刷新」下拉（关闭 / 5 / 15 / 30 / 60 秒，记忆到 localStorage `gw-overview-poll`）+「⟳ 立即刷新」按钮 +「最后刷新」时间戳；当 Diverged 横幅展开时，前端会调用 `/admin-api/runtime/diff` 并按行集合差集估算 `+N / -N` 数量，作为 daisyUI badge 显示在横幅内，方便快速判断分歧规模。
  - **登录页 + 整体 UI 美化**：新增 `/admin/login` 自定义登录页（玻璃拟态卡片 + 渐变背景 + 「记住我 7 天」复选框），后端新增 `POST /admin-api/auth/login` 与 `POST /admin-api/auth/logout`，鉴权同时接受 **Cookie**（`syz_admin_auth`，HttpOnly + Lax，HTTPS 自动 Secure）与 **HTTP Basic**（兼容 curl / 自动化脚本）；中间件对 `/admin-api/*` 未授权返回 401 JSON（不再触发浏览器原生 popup），对页面访问 302 跳转登录页并保留 `returnUrl`。`_AdminLayout` 全面美化：渐变 navbar + 品牌徽标 + 面包屑、用户头像下拉菜单、daisyUI 多主题切换器（10 种主题，写入 localStorage `gw-admin-theme`）、侧栏图标 + 响应式收纳、卡片淡入动效、`adminFetch` 自动捕获 401 跳转登录页、`adminToast` 优化淡出动画。
  - **登录限流 + 业务页统一美化**：`AuthController` 接入 `IFailedLoginTracker`（默认 `FailedLoginTracker` 内存实现，按客户端 IP 计数），默认 5 次失败后锁定 5 分钟并返回 **429 + Retry-After**，错误提示会告知剩余尝试次数；同时重构 `_AdminLayout` 主区域，为所有业务页统一渲染「品牌图标 + 渐变标题 + 副标题」页头（`ViewData["Subtitle"]`），清理各页重复的 `<h1>`，添加全局卡片/表格柔和阴影与边框、按钮圆角、`thead` 字重调整，视觉在 Routes / Audit / Backups / Diff / RawEditor / RouteOverview / SwaggerEndpoints / Tester / Index 所有页一致。限流阈值 绑定到配置节 `Gateway:Admin:RateLimit`（`MaxFailures` / `LockoutMinutes` / `WindowMinutes`），未配置时使用默认值。反向代理场景可启用 `TrustForwardedFor=true` + `ForwardedHeaderName`（默认 `X-Forwarded-For`）让限流计数以真实客户端 IP 为键；请确保该请求头由受信代理写入。多实例网关可调用 `services.UseSyZeroGatewayAdminCachedRateLimit()` 切换到内置的 `CachedFailedLoginTracker`（基于 `ICache` 抽象，需预先注册 `ICache` 实现如 `SyZero.Redis`）在多实例间共享失败计数与锁定状态。当失败计数 ≥ `CaptchaThreshold`（默认 3，0=关闭）时登录必须携带验证码；服务端引入 `ICaptchaStore` 抽象，默认 `MemoryCaptchaStore`（5 分钟过期 / 一次性消费），添加匯路 `GET /admin-api/auth/captcha` 颁发算术验证码；登录页检测到 `requireCaptcha=true` 后自动加载并呈现验证码输入区。
  - **审计页过滤器 + CSV 导出**：`GET /admin-api/audit` 新增 `user` / `action`（前缀匹配） / `from` / `to`（ISO 8601）查询参数，响应补充 `FilteredCount` / `ScannedCount` / `Truncated`；新增 `GET /admin-api/audit/export.csv`（同样过滤参数，最多 20000 行，UTF-8 BOM 对 Excel 友好）。Audit 页面重构为滤镜栏 + 导出按钮 + 骨架/空态 + 匹配计数提示。
  - **路由热重载 SSE 推送**：新增 `IGatewayAdminEventBus`（Channel 有界队列、`DropOldest` 背压）与 `GET /admin-api/events` 端点；所有写操作（`route.*` / `swagger.*` / `config.*` / `backup.*`）同步广播。前端在 `_AdminLayout` 注入全局 EventSource（指数退避重连、指示灯徽章），路由/Swagger 页面收到事件后自动去抖刷新，其他页面以 toast 呈现。
  - **IP 黑白名单**：新增 `Gateway:Admin:IpFilter`（`Allow` / `Deny` / `TrustForwardedFor` / `ForwardedHeaderName`）与 `AdminIpFilterMiddleware`，在 Auth 之前拦截。支持 IPv4 / IPv6 单 IP 与 CIDR（`10.0.0.0/8` / `2001:db8::/32`），IPv4-mapped IPv6 自动规范化；Deny 优先，Allow 非空才限制。使用 `IOptionsMonitor` 热重载。
  - Basic Auth 中间件成功后写入 `ClaimsPrincipal`，使审计 / 日志能够准确记录用户名。
- **`IGatewayConfigurationStore` 新增 4 个默认接口方法**（向后兼容）：`ListBackupsAsync` / `ReadBackupAsync` / `RestoreBackupAsync` / `DeleteBackupAsync`。`SyZero.Gateway.Ocelot` / `SyZero.Gateway.YARP` 等现有实现 **无需修改** 即可编译；不支持的实现返回 `501 Not Supported`。`SyZero.Gateway.Admin` 内置的 `FileGatewayConfigurationStore` 提供完整实现。

## v1.2.2

- **远程服务调用抽象（破坏性）**：把 `SyZero.Feign` 中与 Feign 自身实现解耦的部分下沉到 `SyZero.Core`，新增 `SyZero.Client.RemoteService` 命名空间下的 `IRemoteServiceProvider`（服务级 Provider）、`IRemoteServiceProxyFactory`（协议级工厂）、`RemoteServiceProxyFactoryManager`、`RemoteServiceOptions` / `RemoteServiceItem` / `RemoteServiceSetting` / `RemoteServiceProtocol`，以及 `services.AddRemoteService<TProvider>(...)` 一组扩展方法。
- **`SyZero.Feign` 改造为 Provider**：`FeignServiceRegistrar` 重构为 `FeignRemoteServiceProvider : IRemoteServiceProvider`，内部仍预注册内置的 `HttpProxyFactory` / `GrpcProxyFactory`；删除 `FeignOptions` / `FeignService` / `ServiceSetting` / `FeignProtocol` / `IFeignProxyFactory` / `FeignProxyFactoryManager`，改为使用 Core 中的对应类型。
- **配置字段重命名**：`Feign.Service[*].DllName` → `Feign.Service[*].AssemblyName`（语义保持一致，仍为接口契约程序集名）。请同步更新 `appsettings.json`。
- **API 形态保留**：`builder.Services.AddSyZeroFeign(...)` 三种重载签名兼容（参数类型由 `FeignOptions` 替换为 `RemoteServiceOptions`），现有调用站点的源码改动只在 `using` 与类型名上。
- **动态 API Provider 抽象下沉到 Core**：在 `SyZero.Core` 新增 `SyZero.Application.DynamicApi` 命名空间，提供 Provider 中立的公共能力 —— `DynamicApiTypeFilter`（统一类型筛选规则）、`DynamicApiNameNormalizer`（按长度倒序的后缀剥离 + 自定义委托）、`DynamicApiAttributeCache`（带缓存的特性查找）、`DynamicApiAssemblyScanner`（默认排除系统程序集 + 可追加排除回调）、`DynamicApiServiceCollectionExtensions`、`DynamicApiTypeHelper`、`DynamicApiOptionsBase` 抽象基类、`IAssemblyDynamicApiOptions` 标记接口。`SyZero.DynamicWebApi` / `SyZero.DynamicGrpc` 内部 `Helpers/ReflectionHelper.cs`、`Helpers/TypeHelper.cs`、`Helpers/ServiceCollectionExtensions.cs` 三处重复实现已删除并改为复用 Core；`DynamicWebApiOptions` / `DynamicGrpcOptions` 继承 `DynamicApiOptionsBase`，公开属性形态保持不变（无破坏性变更）。后续可按相同模式新增其他 Provider（如 GraphQL / MiniAPI / MCP Server / JSON-RPC）。
- **未来扩展**：可按相同模式新增其它 Provider 模块（例如基于 WebApiClient、System.Text.Json 的轻量实现）并与 Feign 共存。

## v1.2.1

- **新模块 [`SyZero.Gateway.YARP`](/modules/SyZero.Gateway.YARP)**：第二份 `IGateway` 后端（基于 [YARP](https://microsoft.github.io/reverse-proxy/) 2.3），与 `SyZero.Gateway.Ocelot` 平行。`AddSyZeroYarp(...)` + `await app.UseSyZeroYarpAsync()` 一行接入，与 Ocelot 共用同一份 `configuration.json` schema（`Routes` / `LoadBalancerOptions` / `ServiceName` 等），可被 `SyZero.Gateway.Admin` 同一面板管理。
- **统一服务发现接入**：新增 `SyZeroYarpServiceDiscoveryDestinationResolver`（YARP 侧）与 `SyZeroDiscovery` 提供器（Ocelot 侧），都基于同一个 `IServiceManagement` 抽象。凡是注册了 Consul / Nacos / Redis / Local / DB 中任一后端，都可以透明接入两种网关。
- **实例上下线类即时反馈**：Resolver 首次解析时自动调用 `IServiceManagement.Subscribe(...)`，通过 `IChangeToken` 仅刷新受影响的 cluster，无需重载整份网关配置。`Dispose` 时自动 `Unsubscribe`。
- **跨后端 LB 策略映射**：`GatewayLoadBalancerOptions.Type`（`RoundRobin` / `LeastConnection` / `NoLoadBalance`）由 `YarpConfigMapper` 自动翻译为 YARP 原生策略（`RoundRobin` / `LeastRequests` / `FirstAlphabetical`），与 Ocelot 侧含义一致，同一份 `configuration.json` 可在两个后端间热切。
- **健康检查双判陶陶**：健康过滤集中在 `IServiceManagement.GetHealthyServices(...)` 源头完成，YARP / Ocelot 默认不再重复做 active probe，避免与服务注册中心两套判定冲突；如需 active probe 可在各后端原生配置中启用。
- **文档**：中英文文档同步为 `SyZero.Gateway.YARP` / `SyZero.Gateway.Ocelot` 增加「服务发现」与「负载均衡与健康检查」章节。

## v1.2.0

- **破坏性变更**：核心 NuGet 包 `SyZero` 重命名为 `SyZero.Core`。请将 `dotnet add package SyZero` 替换为 `dotnet add package SyZero.Core`；程序集名称（`SyZero.dll`）与根命名空间（`SyZero`）保持不变，源代码无需修改 `using` 。
- **仓库重构**：原 `src/SyZero.Core/SyZero.*` 下的 18 个可插拔模块项目迁移至新目录 `src/SyZero.Module/`；`src/SyZero.Core/SyZero/` 内容上提至 `src/SyZero.Core/`。
- **示例项目**：新增 [`SyZero.Example3.SPA`](/guide/example3-spa) 单体宿主示例（React 19 + Vite 6 + Tailwind v4 前端嵌入 ASP.NET Core 9），演示 `SyZero.AspNetCore.SpaProxy` 开发期反代与发布期同源打包。
- **SyZero.Gateway 示例**：新增 `/admin` 网关管理页面（Razor Pages + Tailwind/daisyUI CDN），支持对 `configuration.json` 中 `Routes` 与 `SwaggerEndPoints` 的 CRUD；路由变更通过 `IFileConfigurationSetter` 热生效，写入前自动备份。Basic Auth 凭据读取自 `Gateway:Admin:Username` / `Gateway:Admin:Password`，未配置时返回 503。
- **新模块 [`SyZero.Gateway.Admin`](/modules/SyZero.Gateway.Admin)**：将上述管理面板抽取为独立可发布的 NuGet 包，Razor Pages 以 RCL 形式嵌入 dll，安装后一行 `AddSyZeroGatewayAdmin()` 即可拥有完整面板。
- **网关抽象重构（破坏性）**：将网关能力抽成 `SyZero.Core` 中的 `IGateway` / `IGatewayConfigurationStore` / `IGatewayConfigurationApplier` / `GatewayConfiguration` 抽象（命名空间 `SyZero.Gateway`），原 `SyZero.ApiGateway` 模块更名并重写为 `SyZero.Gateway.Ocelot`（基于 Ocelot 的具体实现）；管理面板模块更名为 `SyZero.Gateway.Admin`，**只依赖核心抽象**，可与任意 `IGateway` 实现共存。后续可按相同模式新增其他网关实现包（如 YARP）。

## v1.1.9

- 修复错误

## v1.1.8

- 修复错误

## v1.1.7

- 修复错误

## v1.1.6

- 新增 MongoDB、Redis 事件总线支持，增强分布式消息能力。
- 增强 SpaProxy：新增 HostedService 与配置支持，完善前端开发启动文档。

## v1.1.5

- 新增 context7.json，统一 URL 与公钥配置。
- 升级并优化 Swagger、AutoMapper 扩展与依赖注入流程。
- 优化 NuGet 工具链，修复包推送逻辑问题。

## v1.1.4

- 新增核心模块、网关、Feign、ORM、消息队列、服务发现等组件，并更新构建配置和文档。

## v1.1.3

- 修复错误

## v1.1.2

- 修复错误

## v1.1.1

- 添加Fegin
