# 发布说明


## v9.1.0

### 运行时与构建

- **TFM 统一为 `net9.0`**：除 `SyZero.SourceGenerator`（Roslyn 限制必须 `netstandard2.0`）外，全部框架模块统一目标框架为 `net9.0`。原本仍为 `netstandard2.1` 的 `SyZero.Core` / `SyZero.Web.Common` / `SyZero.Redis` / `SyZero.SqlSugar` / `SyZero.MongoDB` / `SyZero.EventBus.RabbitMQ` / `SyZero.Logging.Log4Net` / `SyZero.ServiceDiscovery.Consul` / `SyZero.Observability.OpenTelemetry`、以及双目标 `net8.0;net9.0` 的 `SyZero.ObjectMapping.AutoMapper` 全部收敛为 `net9.0`。**下游消费方需运行在 .NET 9 SDK / Runtime 之上**。
- **解决方案文件升级为 `.slnx` 新格式**：`dotnet build SyZero.slnx -c Release` 与 `dotnet test src/SyZero.Tests/SyZero.Tests.csproj` 验证全套 **273/273 通过**。
- **仓库结构同步重组**：18 个可插拔模块项目从旧路径 `src/SyZero.Core/SyZero.*` 迁移到 `src/SyZero.Module/`；原 `src/SyZero.Core/SyZero/` 内容上提到 `src/SyZero.Core/`，与当前模块化目录结构保持一致。

### 破坏性变更

- **核心 NuGet 包 `SyZero` 正式更名为 `SyZero.Core`**：请将 `dotnet add package SyZero` 替换为 `dotnet add package SyZero.Core`。程序集名称 `SyZero.dll` 与根命名空间 `SyZero` 保持不变，绝大多数业务源码无需修改 `using`。
- **3 个轻量模块物理并入 `SyZero.Core`**：以下三个无第三方依赖的基础能力模块不再单独发布 NuGet 包，类型与 API 签名 0 变更：

  | 旧包 / 命名空间 | 新归属 / 命名空间 |
  |---|---|
  | `SyZero.Validation`（`SyZero.Validation.*`） | [`SyZero.Core`](/modules/SyZero.Core) ｜ `SyZero.*` |
  | `SyZero.DistributedLock`（`SyZero.DistributedLock.*`） | [`SyZero.Core`](/modules/SyZero.Core) ｜ `SyZero.*` |
  | `SyZero.Audit`（`SyZero.Audit.*`） | [`SyZero.Core`](/modules/SyZero.Core) ｜ `SyZero.*` |

  - C# 命名空间已合并到 `SyZero.*`；`SyZero.Redis.RedisDistributedLockProvider` 仍然独立。
  - **迁移**：删除 csproj 中对应 `<PackageReference>` 与 `using` 即可；反射调用旧类型全名（如 `SyZero.Audit.AuditLogEntry`）改为 `SyZero.AuditLogEntry`。

- **7 个模块按"能力分组前缀"统一改名**：参照 `SyZero.Gateway.*` 模式将顶层模块下沉到能力命名空间：

  | 旧包 / 命名空间 | 新包 / 命名空间 |
  |---|---|
  | `SyZero.RabbitMQ` | [`SyZero.EventBus.RabbitMQ`](/modules/SyZero.EventBus.RabbitMQ) |
  | `SyZero.Consul` | [`SyZero.ServiceDiscovery.Consul`](/modules/SyZero.ServiceDiscovery.Consul) |
  | `SyZero.Nacos` | [`SyZero.ServiceDiscovery.Nacos`](/modules/SyZero.ServiceDiscovery.Nacos) |
  | `SyZero.Log4Net` | [`SyZero.Logging.Log4Net`](/modules/SyZero.Logging.Log4Net) |
  | `SyZero.AutoMapper` | [`SyZero.ObjectMapping.AutoMapper`](/modules/SyZero.ObjectMapping.AutoMapper) |
  | `SyZero.Mapster` | [`SyZero.ObjectMapping.Mapster`](/modules/SyZero.ObjectMapping.Mapster) |
  | `SyZero.OpenTelemetry` | [`SyZero.Observability.OpenTelemetry`](/modules/SyZero.Observability.OpenTelemetry) |

  - 包 ID 与 C# 命名空间同步变更，无 meta-package 兼容包，旧包 ID 停发。`Add*` / `Use*` 扩展方法签名不变。

- **网关抽象层独立成包 [`SyZero.Gateway.Abstractions`](/modules/SyZero.Gateway.Abstractions)**：原位于 `SyZero.Core/Gateway/` 的 11 个类型（`IGateway` / `IGatewayConfigurationStore` / `IGatewayConfigurationApplier` / `GatewayConfiguration` / `GatewayRoute` / `GatewaySwaggerEndPoint` / `GatewayOptions` / `GatewayLoadBalancerOptions` / `GatewayRateLimitOptions` / `GatewayGlobalRateLimitOptions` / `GatewayConfigurationBackup`）抽出。原 `SyZero.Ocelot` → [`SyZero.Gateway.Ocelot`](/modules/SyZero.Gateway.Ocelot)；原 `SyZero.YARP` → [`SyZero.Gateway.YARP`](/modules/SyZero.Gateway.YARP)。**抽象层命名空间保留为 `SyZero.Gateway`**，业务 `using` 无需修改；`SyZeroOcelotOptions.CorsPolicyName` / `SyZeroYarpOptions.CorsPolicyName` 默认值同步更新。

- **远程服务调用抽象下沉到 Core**：`SyZero.Feign` 中协议无关部分迁入 `SyZero.Core` 的 `SyZero.Client.RemoteService` 命名空间（`IRemoteServiceProvider` / `IRemoteServiceProxyFactory` / `RemoteServiceOptions` / `RemoteServiceProtocol` 等）；`SyZero.Feign` 重构为 `FeignRemoteServiceProvider : IRemoteServiceProvider` 实现。配置字段 `Feign.Service[*].DllName` → `AssemblyName`，`AddSyZeroFeign(...)` 重载签名兼容（参数类型由 `FeignOptions` 替换为 `RemoteServiceOptions`）。

- **动态 API Provider 抽象下沉到 Core**：在 `SyZero.Core` 新增 `SyZero.Application.DynamicApi` 命名空间，提供 Provider 中立的 `DynamicApiTypeFilter` / `DynamicApiNameNormalizer` / `DynamicApiAttributeCache` / `DynamicApiAssemblyScanner` / `DynamicApiOptionsBase` 等公共能力。`SyZero.DynamicWebApi` / `SyZero.DynamicGrpc` 内部重复实现已删除并改为复用 Core；公开属性形态保持不变（无破坏性变更）。

- **可空返回值收紧**：`IObjectMapper.Map<T>` / `IJsonSerialize.ObjectToJSON` / `JSONToObject<T>` / `ICache.Get<T>` / `IToken.GetPrincipal` / `IXmlSerialize` 一组接口的返回值改为可空，启用 `<Nullable>enable</Nullable>` 后会得到编译期警告，请按需补 null 检查。

### Core 增强

- **泛型主键支持（向后兼容）**：新增 `IEntity<TKey>` / `Entity<TKey>` 与 `IRepository<TEntity, TKey>`；现有 `IEntity` / `Entity` / `IRepository<TEntity>` 通过继承保留默认 `long` 主键。EF Core / SqlSugar / MongoDB 三大 Provider 同步泛化，可定义 `Guid` / `string` / 复合 ID 等任意主键。
- **多级缓存（L1+L2 + 防穿透/击穿/雪崩）**：新增 `IMultiLevelCache` + `MultiLevelCache` 默认实现（L1 = `IMemoryCache`，L2 = `IDistributedCache`）。**防穿透**：`NullSentinel` 缓存空值；**防击穿**：`ConcurrentDictionary<key, Lazy<Task>>` SingleFlight；**防雪崩**：`JitterRatio` 给过期时间加随机抖动。`AddSyZeroMultiLevelCache(...)` 一行注册；`SyZero.Redis` 自动提供 `RedisDistributedCache` 作为 L2。
- **配置热更新通知**：新增 `IConfigurationChangedNotifier`（`event Changed`）+ 默认实现，基于 `Microsoft.Extensions.Primitives.ChangeToken` 监听 `IConfiguration.GetReloadToken()`，对 **任意配置源**（appsettings.json / Nacos / Consul / Apollo 等）生效。

### 认证授权与多租户

- **新模块 [`SyZero.Authorization`](/modules/SyZero.Authorization)**：基于 ASP.NET Core Identity 的 RBAC + JWT Bearer 认证授权一站式实现。包含 `User` / `Role` / `PermissionGroup` / `Permission` / `UserRole` / `RolePermission` / `UserPermission` / `RefreshToken` 实体；`IPermissionDefinitionProvider` + `PermissionDefinitionManager` + `PermissionSyncHostedService` 启动时同步落库；`RbacPermissionChecker` 用户级显式 deny 立即生效；`JwtTokenManager`（HMAC-SHA256, `ClockSkew=30s`）+ `RefreshTokenManager`（DB 仅存 SHA-256 hash，重放检测级联撤销）；`SyZeroAccountManager` 提供 `LogoutAsync` / `LogoutAllAsync` / `ChangePasswordAsync` / `ResetPasswordAsync`，改密后自动全终端下线。`AddSyZeroAuthorization(builder.Configuration)` + `app.UseSyZeroAuthorization()` 一行接入。
- **新模块 [`SyZero.Authorization.AspNetCore`](/modules/SyZero.Authorization.AspNetCore)**：`SyPermissionRequirement` + `SyPermissionAuthorizationHandler` 把 `[Authorize(Policy = "syperm:Order.Create")]` 桥接到 `IPermissionChecker`。
- **新模块 [`SyZero.Authentication.ApiKey`](/modules/SyZero.Authentication.ApiKey)**：最小化 API Key 认证 Scheme（请求头白名单 → 主体名映射），适合服务间调用。
- **新模块 [`SyZero.MultiTenancy`](/modules/SyZero.MultiTenancy)**：多租户上下文 + 解析中间件。`ICurrentTenant`（`AsyncLocal<TenantInfo?>`）；`HeaderTenantResolver` / `DomainTenantResolver` / `ClaimTenantResolver` 三种 Resolver；`ITenantStore` 抽象 + `InMemoryTenantStore` 演示；`IMultiTenant` 实体标记（`long? TenantId`）；`ITenantConnectionResolver` 分库扩展点。
- **新模块 [`SyZero.MultiTenancy.EntityFrameworkCore`](/modules/SyZero.MultiTenancy.EntityFrameworkCore)**：`TenantEntity` + `TenantDbContext`（`SyTenants` 表 + `Name` 唯一索引）+ `EfCoreTenantStore`。

### 后台任务与分布式事务

- **新模块 [`SyZero.BackgroundJobs`](/modules/SyZero.BackgroundJobs)**：基于 `System.Threading.Channels` 的内存后台任务。`IBackgroundJob<TArgs>` + `IBackgroundJobManager.EnqueueAsync<TJob, TArgs>(args)`；`BackgroundJobChannel` 默认 1024 容量，可配 `DropOldestWhenFull`；`BackgroundJobWorker` 可配 `WorkerCount` 并发度，任务异常不影响其他任务。
- **新模块 [`SyZero.BackgroundJobs.Quartz`](/modules/SyZero.BackgroundJobs.Quartz)**：基于 Quartz 的 Cron 调度后端，`AddSyZeroBackgroundJobsQuartz(q => q.AddSyZeroCron<TJob, TArgs>(cron, args))` 一行注册。
- **新模块 [`SyZero.DistributedTransaction`](/modules/SyZero.DistributedTransaction)**：Outbox 模式 + 简易 Saga 编排器。`IOutboxStore` + `OutboxBackgroundService`（`PollIntervalSeconds` / `BatchSize` / `MaxRetryCount` 可配）；`ISagaStep<TContext>` + `SagaCoordinator<TContext>` 顺序执行 + 反向补偿，结果以 `SagaResult` 返回。
- **新模块 [`SyZero.DistributedTransaction.EntityFrameworkCore`](/modules/SyZero.DistributedTransaction.EntityFrameworkCore)**：`EfCoreOutboxStore` + `OutboxDbContext`（`SyOutboxMessages` 表 + `PublishedAtUtc` 索引）。
- **新模块 [`SyZero.Idempotency`](/modules/SyZero.Idempotency)**：基于 `Idempotency-Key` 请求头的接口幂等中间件，仅命中 POST / PUT / PATCH / DELETE，复用首次响应（StatusCode / ContentType / Body）。

### 异常处理与可观测性

- **新增 ASP.NET Core 全局异常 + ProblemDetails (RFC 7807)**：`SyError` + `SyBusinessException` + `SyExceptionMiddleware` 把业务异常落为 `application/problem+json`（含 `traceId`），未捕获异常落为 `SY-500`；`AddSyZeroExceptionHandler()` + `UseSyZeroExceptionHandler()` 一行接入。
- **新模块 [`SyZero.HealthChecks`](/modules/SyZero.HealthChecks)**：K8s 友好的三档端点（`/health/live`、`/health/ready`、`/health`）。
- **新模块 [`SyZero.Diagnostics`](/modules/SyZero.Diagnostics)**：请求诊断中间件，结构化日志记录 method / path / status / 耗时 / TraceId / UserId / TenantId。
- **新模块 [`SyZero.RateLimiting`](/modules/SyZero.RateLimiting)**：包装 .NET 内置 `System.Threading.RateLimiting` + ASP.NET Core 中间件，默认按客户端 IP 注册 `syzero-global` 固定窗口策略；提供 `IDistributedRateLimiter` 抽象与 `LocalDistributedRateLimiter` 默认实现。
- **新模块 [`SyZero.DataProtection`](/modules/SyZero.DataProtection)**：`[Encrypted("purpose")]` 标记 + `IFieldEncryptor` 抽象 + 默认 `DataProtectorFieldEncryptor`（包装 ASP.NET Core `IDataProtectionProvider`，不同 Purpose 隔离密钥）。

### 弹性、消息与生态适配

- **新模块 [`SyZero.Resilience`](/modules/SyZero.Resilience)**：Polly v8 包装。`SyResiliencePipelines.Default(...)` 一行获取「超时 → 重试（指数退避 + 抖动）→ 熔断（50% 失败率）」默认管道。
- **新模块 [`SyZero.EventBus.Kafka`](/modules/SyZero.EventBus.Kafka)**：基于 Confluent.Kafka 的生产者 / 消费者骨架。`SyKafkaConsumerBase` 派生即用，处理失败按「主 → `*.retry` → `*.dlq`」三段式自动转发；生产者启用幂等（`EnableIdempotence=true` / `Acks=All`）。重试消息携带 `sy-not-before` header 实现 scheduled retry；退避策略抽出 `SyKafkaBackoff.Compute(retryCount, baseDelay, maxDelay)`（指数退避 + ±20% 抖动 + 上限封顶）。
- **新模块 [`SyZero.Sdk`](/modules/SyZero.Sdk)**：`SyServiceClient` typed-HttpClient 基类 + `AddSyServiceClient<TClient>(baseAddress)` 一行注册；自带 `GetAsync<T>` / `PostAsync<TReq,TResp>` JSON 快捷方法。
- **新模块 [`SyZero.Realtime`](/modules/SyZero.Realtime)**：基于 ASP.NET Core SignalR 的 `SyRealtimeHub` + `ISyRealtimeNotifier` 抽象。
- **新模块 [`SyZero.Files`](/modules/SyZero.Files)**：`ISyFileStorage` 抽象 + `LocalFileStorage` 本地磁盘默认实现（内置目录穿越防御）。
- **新模块 [`SyZero.Mail`](/modules/SyZero.Mail)**：`ISyMailSender` 抽象 + `SmtpMailSender` + `ConsoleMailSender`。
- **新模块 [`SyZero.Sms`](/modules/SyZero.Sms)**：`ISySmsSender` 抽象 + `ConsoleSmsSender` 占位实现。
- **新模块 [`SyZero.FeatureManagement`](/modules/SyZero.FeatureManagement)**：包装 `Microsoft.FeatureManagement`，提供 `ISyFeatureManager.IsEnabledAsync(feature)` 抽象。
- **新模块 [`SyZero.Localization`](/modules/SyZero.Localization)**：包装 `Microsoft.Extensions.Localization`，提供 `ISyStringLocalizer["key"]` 抽象。
- **新模块 [`SyZero.SourceGenerator`](/modules/SyZero.SourceGenerator)**：Roslyn 增量生成器骨架。`SyServiceMarkerGenerator` 扫描 `IApplicationService` 类型生成 `SyDiscoveredServices.Names`；`SyDIRegistrationGenerator` 扫描实现 `IScopedDependency` / `ISingletonDependency` / `ITransientDependency` 的非抽象类，生成 `AddSyZeroGenerated(IServiceCollection)` 扩展方法以替代运行时反射扫描。`netstandard2.0`，以 analyzer 形式被消费。
- **新模块 [`SyZero.GraphQL`](/modules/SyZero.GraphQL)**：HotChocolate 包装。`AddSyZeroGraphQL(b => ...)` + `MapSyZeroGraphQL("/graphql")` 一行接入。
- **新模块 [`SyZero.AI`](/modules/SyZero.AI)**：`ISyChatClient` 抽象 + `EchoChatClient` 占位实现 + `MeaiChatClientAdapter` 把任意 `Microsoft.Extensions.AI.IChatClient`（OpenAI / Azure OpenAI / Ollama 等）适配为 `ISyChatClient`。

### 网关示例与 Admin 面板

- **示例项目新增 [`SyZero.Example3.SPA`](/guide/example3-spa)**：单体宿主示例将 React 19 + Vite 6 + Tailwind v4 前端嵌入 ASP.NET Core 9，演示 `SyZero.AspNetCore.SpaProxy` 在开发期反代与发布期同源打包。
- **`SyZero.Gateway` 示例内置 `/admin` 管理页**：Razor Pages + Tailwind/daisyUI 方式提供 `configuration.json` 中 `Routes` 与 `SwaggerEndPoints` 的 CRUD，路由变更通过 `IFileConfigurationSetter` 热生效，写入前自动备份；`Gateway:Admin:Username` / `Gateway:Admin:Password` 未配置时返回 503。
- **[`SyZero.Gateway.Admin`](/modules/SyZero.Gateway.Admin) 作为独立模块发布并持续增强**：管理面板以 Razor Class Library 形式嵌入，接入端可通过 `AddSyZeroGatewayAdmin()` 一行启用，并且仅依赖网关抽象层，可与不同 `IGateway` 实现组合。
- **新模块 [`SyZero.Gateway.YARP`](/modules/SyZero.Gateway.YARP)**：第二份 `IGateway` 后端（基于 [YARP](https://microsoft.github.io/reverse-proxy/) 2.3），与 `SyZero.Gateway.Ocelot` 平行，共用同一份 `configuration.json` schema。
- **统一服务发现接入**：`SyZeroYarpServiceDiscoveryDestinationResolver`（YARP 侧）与 `SyZeroDiscovery` 提供器（Ocelot 侧）均基于同一个 `IServiceManagement` 抽象，支持 Consul / Nacos / Redis / Local / DB 任一后端。Resolver 通过 `IChangeToken` 仅刷新受影响 cluster，无需重载整份网关配置。
- **跨后端 LB 策略映射**：`GatewayLoadBalancerOptions.Type`（`RoundRobin` / `LeastConnection` / `NoLoadBalance`）由 `YarpConfigMapper` 自动翻译为 YARP 原生策略（`RoundRobin` / `LeastRequests` / `FirstAlphabetical`），同一份 `configuration.json` 可在两个后端间热切。
- **`SyZero.Gateway.Admin` 全面增强**：完整 Razor Pages 管理面板。
  - **多用户 + 角色（`Admin` / `Reader`）**：保留旧版单账号配置，同时新增 `Gateway:Admin:Users[]` 多账号 + 角色；`Reader` 仅 `GET`，写操作返回 403（例外允许 logout 与改自己密码）。`IGatewayAdminUserStore` + `FileGatewayAdminUserStore`（原子写、`SemaphoreSlim` 串行化）；`IsBuiltIn=true` 不可改不可删，最后一个 Admin 不可降级 / 删除。
  - **路由限流（Ocelot）**：`GatewayRoute.RateLimitOptions`（`Period` / `PeriodTimespan` / `Limit` / `ClientWhitelist[]`）+ `GET/PUT/DELETE /admin-api/routes/{i}/rate-limit`。
  - **全局限流**：`GatewayConfiguration.GetGlobalRateLimit()` / `SetGlobalRateLimit(value?)` 桥接 `Extra["GlobalConfiguration"]["RateLimitOptions"]`，保留同节点其它字段。
  - **路由负载均衡**：`GET/PUT /admin-api/routes/{i}/load-balancer`，仅接受 `RoundRobin` / `LeastConnection` / `NoLoadBalance`。
  - **批量启停 + 标签 + 拖拽排序**：`Disabled` / `Tags` / `Description` 字段；批量端点 `bulk-toggle` / `bulk-tags` / `reorder`；YAML 导入导出（基于 YamlDotNet 16）；Markdown 描述渲染（marked 12）；路由概览页（按 `ServiceName` 分组统计 + Chart.js 4.4.4 环图/柱状图）。
  - **路由测试器 + 收藏夹**：纯浏览器侧 fetch；URL 查询参数预填；最近 20 条 localStorage；收藏夹 + cURL 互转（内置 shell 词法解析）。
  - **备份 / 原生 JSON 编辑器 / 运行时 Diff / 审计 / SSE 热推送**：`configuration.backup.*.json` 生命周期管理（`MaxCount` 默认 10）；`/admin-api/configuration` 全量替换；`/admin-api/runtime/diff` 行级高亮 + diff badge；`IGatewayAdminAuditLog` JSONL 落盘（`<ContentRoot>/logs/gateway-admin-audit.jsonl`）+ 滤镜栏 / CSV 导出；`IGatewayAdminEventBus` Channel 有界队列广播写操作，前端 EventSource 自动重连。
  - **登录页 + 限流 + 验证码 + IP 黑白名单**：`/admin/login` 玻璃拟态登录页（Cookie + Basic 双协议）；`IFailedLoginTracker`（默认按 IP 计数，5 次失败锁 5 分钟，返回 429 + Retry-After）+ `CachedFailedLoginTracker`（基于 `ICache` 抽象多实例共享）；`ICaptchaStore` + `MemoryCaptchaStore`（5 分钟过期 / 一次性消费）`/admin-api/auth/captcha` 算术验证码；`Gateway:Admin:IpFilter` 支持 IPv4 / IPv6 单 IP 与 CIDR 黑白名单（`IOptionsMonitor` 热重载）。
  - **整体 UI 美化**：daisyUI 多主题切换器（10 种主题）、品牌徽标 + 渐变 navbar / 页头副标题、卡片淡入动效、`adminFetch` 自动 401 跳转、`adminToast` 优化淡出。
- **`IGatewayConfigurationStore` 新增 4 个默认接口方法**（向后兼容）：`ListBackupsAsync` / `ReadBackupAsync` / `RestoreBackupAsync` / `DeleteBackupAsync`，不支持的实现返回 `501 Not Supported`。

### 测试覆盖

- 全套单测从 187/187 → 273/273，新增覆盖：多级缓存 / 配置热更新 / 幂等 Store / Outbox / Saga / Authorization.AspNetCore / MultiTenancy.EFCore / Quartz / FeatureManagement / Localization / Resilience / Sdk / Realtime / Files / Notifications / Kafka / DistributedTransaction.EFCore / ApiKey 认证 / Diagnostics / SourceGenerator（含 DI 注册生成器） / GraphQL / AI（含 MEAI 适配器） / HealthChecks / Audit / RateLimiting / Validation / DataProtection / Gateway.Admin（用户 + 限流 + 备份 + 路由批操作 + YAML 导入导出共 88+ 用例）。

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
