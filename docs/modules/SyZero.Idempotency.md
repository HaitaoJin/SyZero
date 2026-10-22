# SyZero.Idempotency

ASP.NET Core 幂等性中间件 + 存储抽象。基于 `Idempotency-Key` 请求头复用之前的响应，保证同一 key 的重复请求只执行一次业务逻辑。

## 📦 安装

```bash
dotnet add package SyZero.Idempotency
```

## ✨ 特性

- 🛡️ **基于 `Idempotency-Key` 头**：业界通用约定（Stripe / GitHub / RFC 草案）。
- 🔄 **响应快照复用**：首次成功响应缓存到 `IIdempotencyStore`，后续请求直接回放。
- ⚡ **并发占位**：首请求落地后立即占位，避免双发请求都执行业务。
- 🔌 **可插拔存储**：默认 `MemoryIdempotencyStore`（单实例 / 测试）；多实例可换 Redis / 数据库实现。
- 🎯 **方法过滤**：默认仅对 POST / PUT / PATCH / DELETE 生效，GET / HEAD 默认幂等不拦截。

---

## 🚀 快速开始

### 1. 注册服务

```csharp
builder.Services.AddSyZeroIdempotency(builder.Configuration);
// 或：builder.Services.AddSyZeroIdempotency(configure: o => o.ExpirationSeconds = 600);
```

### 2. 启用中间件

```csharp
app.UseRouting();
app.UseSyZeroIdempotency(); // 建议放在路由之后、终结点之前
app.MapControllers();
```

### 3. 客户端调用

```http
POST /api/orders
Content-Type: application/json
Idempotency-Key: 5d1a3f08-...-uuid

{ "productId": 1, "quantity": 2 }
```

同一个 `Idempotency-Key` 在过期窗口内重复发送，会**直接复用第一次的响应**（状态码 / Body / ContentType）。

---

## ⚙️ 配置项

```json
{
  "SyZeroIdempotency": {
    "HeaderName": "Idempotency-Key",
    "ExpirationSeconds": 86400,
    "InflightRejectionStatusCode": 409,
    "Methods": [ "POST", "PUT", "PATCH", "DELETE" ]
  }
}
```

| 字段 | 默认 | 说明 |
|---|---|---|
| `HeaderName` | `Idempotency-Key` | 客户端携带幂等 key 的请求头 |
| `ExpirationSeconds` | 86400 | key 的存活时间（秒），过期后允许重新执行 |
| `InflightRejectionStatusCode` | 409 | 首请求**仍在处理**时并发请求返回的状态码 |
| `Methods` | POST/PUT/PATCH/DELETE | 命中的 HTTP 方法（不区分大小写） |

---

## 🧩 自定义存储

实现 `IIdempotencyStore` 接口：

```csharp
public class RedisIdempotencyStore : IIdempotencyStore { /* SET NX 实现 */ }

builder.Services.AddSingleton<IIdempotencyStore, RedisIdempotencyStore>();
builder.Services.AddSyZeroIdempotency();
```

---

## ⚠️ 注意

- 默认 `MemoryIdempotencyStore` **仅适合单实例**；多实例必须替换为 Redis / 数据库实现。
- 中间件会**缓冲整个响应体**（`MemoryStream`），对大响应可能有内存压力，可在业务侧避免对大响应使用幂等 key。
- 仅缓存 2xx / 4xx 响应，5xx 响应**不缓存**（允许后续重试）。
