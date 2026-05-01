# SyZero.Gateway.Admin

SyZero 网关抽象的可视化管理面板。**仅依赖 `SyZero.Core` 中的 `IGateway` / `IGatewayConfigurationStore` / `IGatewayConfigurationApplier`**，不绑定具体网关实现，因此可与 [`SyZero.Gateway.Ocelot`](./SyZero.Gateway.Ocelot) 或将来任何 `IGateway` 实现配合使用。

挂载到宿主项目（一般就是网关项目本身）后，提供 `/admin` Razor Pages UI 与 `/admin-api/*` REST 接口，对网关配置中的 `Routes` 与 `SwaggerEndPoints` 进行 CRUD；写入由当前注册的 `IGatewayConfigurationStore` 负责，并在写入后调用 `IGatewayConfigurationApplier` 触发实现侧热更新（如 Ocelot 的 `IFileConfigurationSetter`）。当宿主未注册具体实现时，模块兜底使用 `FileGatewayConfigurationStore`（写盘 + 备份，最多保留 10 份）。

UI 走 [Tailwind CSS](https://cdn.tailwindcss.com) + [daisyUI](https://daisyui.com) 的 CDN，无需任何前端构建步骤。

## 📦 安装

```bash
dotnet add package SyZero.Gateway.Admin
```

> 同时需要你的网关项目已经引用并启用一种 `IGateway` 实现（例如 [SyZero.Gateway.Ocelot](./SyZero.Gateway.Ocelot)）。

## 🚀 快速开始

```csharp
using SyZero.Gateway.Ocelot;
using SyZero.Gateway.Admin;

var builder = WebApplication.CreateBuilder(args);

builder.AddSyZero();
builder.Services.AddSyZeroOcelot(o => { /* ... */ }, builder.Configuration);
builder.Services.AddSyZeroGatewayAdmin();   // ← 加一行

var app = builder.Build();

app.UseSyZero();
app.UseRouting();

app.UseSyZeroGatewayAdmin();                // ← Basic Auth + /admin* 分支

app.MapControllers();
app.MapRazorPages();

// Ocelot 是终端中间件，必须分支以避免吞掉 /admin、/admin-api。
app.MapWhen(
    ctx => !ctx.Request.Path.StartsWithSegments("/admin")
           && !ctx.Request.Path.StartsWithSegments("/admin-api"),
    branch => branch.UseSyZeroOcelotAsync().GetAwaiter().GetResult());

await app.RunAsync();
```

## 🔐 凭据配置

> v1.5.0 起在保留旧版 `Gateway:Admin:Username` / `Gateway:Admin:Password` 单账号写法的基础上，**新增** `Gateway:Admin:Users[]` 多账号 + 角色 与运行时 `users.json`。两种 appsettings 写法可并存（同名时数组覆盖单账号项），均视为内置账号（`IsBuiltIn=true`，UI 不可改）。

管理员凭据可来自三个来源（启动时合并）：

1. `appsettings.json` 中的 **`Gateway:Admin:Username` + `Gateway:Admin:Password`**（兼容旧版，单账号默认 `Admin` 角色）
2. `appsettings.json` 中的 **`Gateway:Admin:Users[]`**（多账号 + 角色，与单账号项同名时覆盖之）
3. ContentRoot 下的 **`users.json`**（默认路径，可由 `Gateway:Admin:UsersFile` 覆盖；UI / API 可增删改；与上面任一同名时被忽略并写告警）

```jsonc
// 旧版单账号（继续可用）
{
  "Gateway": { "Admin": { "Username": "admin", "Password": "请用强密码" } }
}
```

```jsonc
// 多账号 + 角色（推荐，可与单账号写法并存）
{
  "Gateway": {
    "Admin": {
      "Users": [
        { "Name": "admin",  "Password": "请用强密码", "Role": "Admin" },
        { "Name": "viewer", "Password": "另一个强密码", "Role": "Reader" }
      ]
    }
  }
}
```

可用角色：

| 角色 | 含义 |
|---|---|
| `Admin` | 完全权限（默认值） |
| `Reader` | 只读：`/admin-api/*` 仅允许 `GET`，写操作（POST/PUT/DELETE）一律 `403`；例外允许 `POST /admin-api/auth/logout` 与 `POST /admin-api/me/password`（改自己的密码） |

未指定 `Role` 时按 `Admin` 处理。鉴权同时支持 **Cookie**（`/admin/login` 表单写入）与 **HTTP Basic**（curl / 自动化脚本）。

环境变量覆盖示例：`Gateway__Admin__Users__0__Name=admin`、`Gateway__Admin__Users__0__Password=...`、`Gateway__Admin__Users__0__Role=Admin`。

### 用户管理（v1.5.0 新增）

页面：`GET /admin/users`（仅 Admin 可见编辑入口；Reader 仅可改自己密码）。
REST：

| 方法 + 路径 | 说明 |
|---|---|
| `GET /admin-api/users` | 列出全部账号（带 `IsBuiltIn` 标记，不返回密码） |
| `POST /admin-api/users` | 新增 file 用户。Body：`{ "name", "password", "role" }`；重名返回 `409` |
| `PUT /admin-api/users/{name}` | 改密码 / 角色（`{ password?, role? }`）；最后一个 Admin 不可降级；`IsBuiltIn=true` 不可改 |
| `DELETE /admin-api/users/{name}` | 删除 file 用户；最后一个 Admin / 当前登录用户 / `IsBuiltIn=true` 均不可删 |
| `POST /admin-api/me/password` | 任意登录用户改自己密码。Body：`{ "oldPassword", "newPassword" }` |

文件 `users.json` 由 `FileGatewayAdminUserStore` 维护：写入临时文件后原子 replace，进程内 `SemaphoreSlim` 串行化；不支持跨实例同步（多实例部署请将所有账号收敛到 `Users[]`）。

### 登录失败限流

v1.2.3 起 `/admin-api/auth/login` 启用基于客户端 IP 的内存限流：默认 **5 次失败/15 分钟窗口、锁定 5 分钟**，触发后返回 `429 + Retry-After`。可通过配置覆盖：

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

接口契约：`IFailedLoginTracker`（默认 `FailedLoginTracker` 单例，可被宿主替换为 Redis / 分布式实现）。

> ⚠️ 反向代理后启用 `TrustForwardedFor=true` 时请确保 `ForwardedHeaderName`（默认 `X-Forwarded-For`）由**受信代理**写入并已剥离用户提交值，否则攻击者可伪造该头绕过 IP 锁定。

#### 多实例共享：基于 ICache 的分布式追踪器

v1.2.3 起内置 `CachedFailedLoginTracker`，依赖 `ICache` 抽象（任意实现皆可，最常见是 `SyZero.Redis`）。多实例网关启用后会共享失败计数与锁定状态：

```csharp
builder.Services
    .AddSyZeroRedis(builder.Configuration)               // 注册 ICache（Redis）
    .AddSyZeroGatewayAdmin()                             // 注册管理面板（默认 内存 Tracker）
    .UseSyZeroGatewayAdminCachedRateLimit();             // 替换为 ICache 实现
```

键格式：`gw-admin:rate-limit:{client-key}`，TTL 取「锁定剩余」与「窗口剩余」中较大者。仅在低频写入场景下使用乐观流程，跨实例并发可能极小概率丢失 1 次失败计数（不会破坏锁定状态本身）。

#### 验证码（Captcha）

v1.2.3 起当 `Gateway:Admin:RateLimit:CaptchaThreshold > 0`（默认 3）时，单 IP 失败次数达到该阈值后，登录请求必须额外携带 `CaptchaId` + `CaptchaAnswer`：

| 端点 | 用途 |
|---|---|
| `GET /admin-api/auth/captcha` | 颁发新算术验证码（匿名可访问），返回 `{ id, question }`（如 `7 + 4 = ?`） |

服务端用 `ICaptchaStore` 抽象保存挑战（默认 `MemoryCaptchaStore`，5 分钟过期；可被宿主替换为 Redis 等分布式版本）。校验成功后立即销毁，不可重用。 `CaptchaThreshold=0` 时关闭验证码。

### IP 黑白名单（v1.2.3 新增）

在 Auth 之前由 `AdminIpFilterMiddleware` 拦截 `/admin*` 与 `/admin-api*` 请求：

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

规则：

1. **Deny 优先**：命中即返回 `403`（`/admin-api/*` 返回 JSON `{title}`，页面返回纯文本）。
2. **Allow 非空**：必须命中 Allow 才放行；为空表示「不限制来源」。
3. **两个名单都为空**：跳过判定，零开销。
4. 支持 IPv4 / IPv6 单 IP（如 `1.2.3.4` / `::1`）与 CIDR（如 `10.0.0.0/8` / `2001:db8::/32`）。
5. **IPv4-mapped IPv6**（`::ffff:10.1.2.3`）会被自动解为 IPv4 后再匹配。
6. 反代后取真实 IP：将 `TrustForwardedFor` 置 `true`，并确保 `ForwardedHeaderName` 头由受信代理写入（已剥离用户提交值）。
7. 配置使用 `IOptionsMonitor` 监听变更，热更新无需重启。

## 🌐 路径与接口

### 页面（Razor Pages）

| 路径 | 说明 |
|---|---|
| `GET /admin` | 概览页：健康 / 统计 / Provider 徽章 / 配置发散告警 |
| `GET /admin/routes` | 路由 CRUD（搜索 / 上下移 / 克隆 / ServiceName 自动补全 / 拖拽排序 / Markdown 描述 / YAML 导入导出 / 列显示设置 / 表头点击三态排序 / 一键发送到测试器） |
| `GET /admin/route-overview` | 路由概览：按 ServiceName 分组统计、标签 / 方法分布 |
| `GET /admin/swagger-endpoints` | Swagger 端点 CRUD |
| `GET /admin/backups` | 备份列表，支持下载 / 还原 / 删除 |
| `GET /admin/raw-editor` | 原生 JSON 编辑器（导入 / 导出 / 全量替换） |
| `GET /admin/diff` | 磁盘 vs 运行时配置的行级 Diff |
| `GET /admin/audit` | 审计日志查看器 |
| `GET /admin/users` | **v1.5.0 新增** 用户与角色管理（仅 Admin 可写） |
| `GET /admin/rate-limiting` | **v1.5.0 新增** 全局 + 单路由限流配置 |
| `GET /admin/load-balancing` | **v1.5.0 新增** 单路由负载均衡器类型管理 |
| `GET /admin/tester` | 浏览器侧路由测试器（直发 fetch，不经管理 API），支持 URL 查询参数 `?method=&url=&headers=&body=` 预填表单，自动保存最近 20 条历史到 localStorage；Routes 页操作列「▶」一键发送；同时提供「收藏夹」（按分组列表，一键回填 / 转 cURL）以及 cURL 命令双向互转、收藏夹 JSON 导入导出 |
| `GET /admin/login` | **v1.2.3 新增** 美化登录页（玻璃拟态 + 渐变背景，支持「记住我 7 天」） |
| `GET /admin/logout` | 清除登录 cookie 并跳转登录页 |

### 管理 REST 接口（`/admin-api/*`）

| 方法 + 路径 | 说明 |
|---|---|
| `GET /admin-api/health` | 版本 / 配置路径 / 最近备份时间 / 统计 / `AuditEnabled` |
| `POST /admin-api/reload` | 强制从磁盘重载并下发 |
| `GET /admin-api/configuration[?download=1]` | 导出全量 JSON（`download=1` 触发文件下载） |
| `PUT /admin-api/configuration` | 全量替换（自动备份当前版本） |
| `GET /admin-api/runtime` | 运行时元信息：`ProviderName`/`StoredHash`/`RuntimeHash`/`Diverged` |
| `GET /admin-api/runtime/diff` | `Stored` 与 `Runtime` 两份序列化 JSON，供前端 Diff |
| `GET/POST/PUT/DELETE /admin-api/routes[/{index:int}]` | 路由 CRUD |
| `POST /admin-api/routes/{index}/move` | Body: `{ "direction": "up"|"down" }` |
| `POST /admin-api/routes/{index}/clone` | 克隆，`UpstreamPathTemplate` 自动追加 `-copy` |
| `POST /admin-api/routes/bulk-toggle` | 批量启停。Body：`{ "indexes": [0,1,2], "disabled": true }` |
| `POST /admin-api/routes/bulk-tags` | 批量打标签。Body：`{ "indexes": [0,1], "tags": ["public"], "op": "set|add|remove" }` |
| `GET /admin-api/routes/tags` | 当前所有已用标签（升序） |
| `POST /admin-api/routes/reorder` | 拖拽重排。Body：`{ "order": [3,0,1,2] }`（必须是 0..n-1 的排列） |
| `GET /admin-api/routes/export?format=json\|yaml` | 仅导出路由列表，默认 JSON；yaml 走 YamlDotNet 序列化 |
| `POST /admin-api/routes/import?format=json\|yaml&mode=append\|replace` | Body 为路由列表文本。`format` 为空时按首字符自动识别；`mode` 默认 append |
| `GET/POST/PUT/DELETE /admin-api/swagger-endpoints[/{key}]` | Swagger 端点 CRUD |
| `GET /admin-api/swagger-endpoints/export?format=json\|yaml` | 仅导出 Swagger 端点列表 |
| `POST /admin-api/swagger-endpoints/import?format=json\|yaml&mode=append\|merge\|replace` | Body 为端点列表文本；`append` 拒绝 Key 冲突，`merge` 按 Key 覆盖/新增，`replace` 全量覆盖 |
| `GET /admin-api/services` | 服务发现的服务名列表（来自 `IServiceManagement`，未注册时返回空+`Available=false`） |
| `GET /admin-api/backups` | 备份列表（`Id`/`CreatedAt`/`SizeBytes`） |
| `GET /admin-api/backups/{id}/download` | 下载备份文件 |
| `POST /admin-api/backups/{id}/restore` | 用备份覆盖当前配置（写入前再次自动备份） |
| `DELETE /admin-api/backups/{id}` | 删除指定备份 |
| `GET /admin-api/audit?take=200&user=&action=&from=&to=` | 读取最近 N 条审计记录，支持按用户名（精确）、动作前缀（如 `route.`）、时间范围（ISO 8601）过滤 |
| `GET /admin-api/audit/export.csv?user=&action=&from=&to=` | **v1.2.3 新增** 按相同过滤条件导出 CSV（最多 20000 行 / UTF-8 BOM，Excel 兼容） |
| `GET /admin-api/events` | **v1.2.3 新增** SSE 长连接，广播写操作事件（`route.*` / `swagger.*` / `config.*` / `backup.*`），用于多面板实时同步；每 15s 心跳 |
| `GET/PUT/DELETE /admin-api/routes/{index}/rate-limit` | **v1.5.0 新增** 单路由限流（Ocelot RateLimitOptions：`EnableRateLimiting` / `Period`（如 `1s`/`5m`/`1h`/`1d`）/ `PeriodTimespan` / `Limit` / `ClientWhitelist[]`） |
| `GET/PUT/DELETE /admin-api/rate-limit/global` | **v1.5.0 新增** 全局限流（`GlobalConfiguration.RateLimitOptions`：`DisableRateLimitHeaders` / `HttpStatusCode`（100–599）/ `QuotaExceededMessage` / `ClientIdHeader`） |
| `GET/PUT /admin-api/routes/{index}/load-balancer` | **v1.5.0 新增** 单路由负载均衡（`{ "type": "RoundRobin\|LeastConnection\|NoLoadBalance" }`） |
| `GET /admin-api/load-balancer/types` | **v1.5.0 新增** 返回支持的负载均衡器类型字符串数组 |
| `POST /admin-api/auth/login` | **v1.2.3 新增** Body：`{ "username", "password", "remember", "captchaId?", "captchaAnswer?" }`；成功写入 `syz_admin_auth` cookie（HttpOnly + Lax，HTTPS 自动 Secure），`remember=true` 时 7 天过期；连续失败达到 `CaptchaThreshold` 后必须携带验证码字段 |
| `POST /admin-api/auth/logout` | **v1.2.3 新增** 清除登录 cookie |
| `GET /admin-api/auth/captcha` | **v1.2.3 新增** 颁发新算术验证码，返回 `{ id, question }` |

> 备份/审计的可选能力由 `IGatewayConfigurationStore` 的默认接口方法承载（详见下文「抽象扩展」），不支持的实现会返回 `501 Not Supported`。

## 🧩 抽象扩展（向后兼容）

`IGatewayConfigurationStore` 在 1.x 系列内补充了 4 个**默认接口方法**：

```csharp
Task<IReadOnlyList<GatewayConfigurationBackup>> ListBackupsAsync(CancellationToken ct = default);
Task<GatewayConfiguration?> ReadBackupAsync(string id, CancellationToken ct = default);
Task RestoreBackupAsync(string id, CancellationToken ct = default);
Task DeleteBackupAsync(string id, CancellationToken ct = default);
```

- 现有实现（如 `OcelotGatewayConfigurationStore` / `YarpGatewayConfigurationStore`）**无需修改**即可编译——默认实现返回空列表或抛 `NotSupportedException`。
- 模块兜底的 `FileGatewayConfigurationStore` 提供完整实现：从磁盘上的 `configuration.backup.*.json` 读取/还原/删除。

## 🗃️ 备份策略

每次写入会先把当前 `configuration.json` 复制为 `configuration.backup.{yyyyMMddHHmmss}.json`，默认保留最近 10 份，可通过配置覆盖：

```json
{
  "Gateway": {
    "Admin": {
      "Backup": { "MaxCount": 30 }
    }
  }
}
```

## 📜 审计日志

所有写操作（路由 / Swagger / 配置 / 备份还原与删除）都会写入 JSONL 审计文件，默认开启：

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

- 默认路径：`<ContentRoot>/logs/gateway-admin-audit.jsonl`
- 单条目字段：`Timestamp`(UTC) / `User` / `Ip` / `Action` / `Target` / `Summary`
- `Action` 取值示例：`route.create`、`route.update`、`route.delete`、`route.move`、`route.clone`、`swagger.create/update/delete`、`config.reload`、`config.replace`、`backup.restore`、`backup.delete`
- 默认实现是简单的全量读取 + 截取最后 N 行，**不做轮转**——大流量场景建议接管 `IGatewayAdminAuditLog` 自行实现。

## ⚠️ 注意事项

- **Razor Pages 来自类库**：模块通过 `Microsoft.NET.Sdk.Razor` 把视图嵌入 dll，宿主无需任何 `Pages/` 文件。
- **必须在 Ocelot 之前注册**：`UseSyZeroGatewayAdmin` 与 `MapControllers/MapRazorPages` 都要排在 Ocelot 接管 pipeline 之前。
- **Swagger 端点修改不会热更新**：`MMLib.SwaggerForOcelot` 只在启动时读取 `SwaggerEndPoints`，UI 中也已用横幅提示。修改后请重启进程或所属容器。
- **`configuration.json` 中的 `//` 注释会在保存时丢失**（System.Text.Json 不写注释）。如需保留行内注释，请走 `git diff` 审查变更。
- **生产部署务必启用 HTTPS**：Basic Auth 在明文 HTTP 上等同于裸密码，请配合反向代理或 Kestrel 直连 TLS 使用。

