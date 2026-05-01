# SyZero.DistributedLock

SyZero 分布式锁模块。`IDistributedLockProvider` 抽象 + 进程内默认实现。Redis 实现位于 [`SyZero.Redis`](/modules/SyZero.Redis) 模块（避免循环依赖）。

## 设计概览

| 能力 | 实现 |
|---|---|
| 抽象 | `IDistributedLockProvider.AcquireAsync(key, expiration, waitTimeout, ct)` 返回 `IAsyncDisposable?`（成功返回锁句柄，超时 / 取消返回 `null`） |
| 进程内默认 | `LocalDistributedLockProvider`（基于 `SemaphoreSlim` + 过期保护）。**仅适合单实例部署 / 测试**。 |
| Redis 实现 | `SyZero.Redis.RedisDistributedLockProvider`（`SET NX PX` + Lua 释放脚本）。引入 `SyZero.Redis` 时**自动注册为默认 `IDistributedLockProvider`**。 |

> 设计目标：调用方不感知后端，业务侧只依赖 `IDistributedLockProvider`，部署期切换 NuGet 包即可在 Local ↔ Redis 之间切换。

## 快速接入

```csharp
// 单实例 / 本地开发
builder.Services
    .AddSyZero()
    .AddSyZeroDistributedLock();   // 注册 LocalDistributedLockProvider

// 多实例生产：只需引入 SyZero.Redis，IDistributedLockProvider 会被覆盖为 Redis 版
builder.Services
    .AddSyZero()
    .AddSyZeroRedis(builder.Configuration);   // 内部 TryAddSingleton 已注册 RedisDistributedLockProvider
```

## 业务用法

```csharp
public class StockAppService : SyZeroServiceBase
{
    private readonly IDistributedLockProvider _lock;
    public StockAppService(IDistributedLockProvider @lock) => _lock = @lock;

    public async Task DeductAsync(long skuId, int qty)
    {
        // 期望 5 秒内拿到锁，最长持有 10 秒（持有者崩溃也会自动释放）
        await using var handle = await _lock.AcquireAsync(
            key: $"stock:{skuId}",
            expiration: TimeSpan.FromSeconds(10),
            waitTimeout: TimeSpan.FromSeconds(5));

        if (handle == null)
            throw new SyMessageException("正忙，请稍后再试");

        // 临界区
        await DeductCoreAsync(skuId, qty);
    }
}
```

| 参数 | 含义 |
| --- | --- |
| `key` | 锁名（建议带业务前缀，如 `stock:1001`） |
| `expiration` | 锁过期时间。持有者崩溃时由后端自动释放，避免死锁。 |
| `waitTimeout` | 获取锁的最长等待时间。`TimeSpan.Zero` 表示「不等待，直接尝试一次」。 |
| 返回值 `IAsyncDisposable?` | `null` 表示等待超时；非空表示已持锁，`DisposeAsync` 释放。 |

## 行为约定

- **可重入**：本模块**不保证可重入**（同一线程多次 `AcquireAsync(同 key)` 会自我阻塞）。如有需要请在业务侧用 `AsyncLocal` 自行包裹。
- **自动释放兜底**：即使忘记 `await using`，锁也会在 `expiration` 后被后端自动回收。
- **释放幂等**：`DisposeAsync` 多次调用安全；释放时检查 token，**不会误删别人的锁**（Redis 实现走 Lua 校验）。

## 兼容性

- `SyZero.DistributedLock`：`netstandard2.1`，依赖 `SyZero.Core`
- `RedisDistributedLockProvider`：随 `SyZero.Redis` 一起发布，无独立 NuGet 包
- 数据库锁实现（基于现有 `IServiceManagement` 用的 DB 表）规划中。
