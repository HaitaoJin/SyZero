# SyZero.DistributedLock

SyZero distributed lock module: `IDistributedLockProvider` abstraction + in-process default. The Redis implementation lives in [`SyZero.Redis`](/en/modules/SyZero.Redis) (to avoid a circular dependency).

## Design

| Capability | Implementation |
| --- | --- |
| Abstraction | `IDistributedLockProvider.AcquireAsync(key, expiration, waitTimeout, ct)` returns `IAsyncDisposable?` (lock handle on success; `null` on timeout / cancellation) |
| In-process default | `LocalDistributedLockProvider` (built on `SemaphoreSlim` + expiration safety net). **Single-instance / test only**. |
| Redis | `SyZero.Redis.RedisDistributedLockProvider` (`SET NX PX` + Lua release script). Registered as the default `IDistributedLockProvider` automatically when `SyZero.Redis` is referenced. |

## Quick start

```csharp
// Single-instance / local development
builder.Services
    .AddSyZero()
    .AddSyZeroDistributedLock();

// Multi-instance production: just add SyZero.Redis;
// IDistributedLockProvider is overridden by the Redis impl
builder.Services
    .AddSyZero()
    .AddSyZeroRedis(builder.Configuration);
```

## Usage

```csharp
public class StockAppService : SyZeroServiceBase
{
    private readonly IDistributedLockProvider _lock;
    public StockAppService(IDistributedLockProvider @lock) => _lock = @lock;

    public async Task DeductAsync(long skuId, int qty)
    {
        await using var handle = await _lock.AcquireAsync(
            key: $"stock:{skuId}",
            expiration: TimeSpan.FromSeconds(10),
            waitTimeout: TimeSpan.FromSeconds(5));

        if (handle == null)
            throw new SyMessageException("System is busy, please try again");

        await DeductCoreAsync(skuId, qty);
    }
}
```

| Parameter | Meaning |
| --- | --- |
| `key` | Lock name (use a business prefix, e.g. `stock:1001`) |
| `expiration` | Lock TTL. Auto-released by the backend if the holder crashes — prevents deadlocks. |
| `waitTimeout` | Max time to wait for acquisition. `TimeSpan.Zero` means try once and return immediately. |
| Return value `IAsyncDisposable?` | `null` = timed out; non-null = lock held; `DisposeAsync` releases it. |

## Behavior

- **Non-reentrant**: acquiring the same key twice from the same logical flow self-blocks. Wrap with `AsyncLocal` if you need reentrancy.
- **Auto-release safety net**: even if you forget `await using`, the backend reclaims the lock after `expiration`.
- **Idempotent release**: multiple `DisposeAsync` calls are safe; the Redis impl uses a Lua script to verify the token before deleting (won't delete someone else's lock).

## Compatibility

- `SyZero.DistributedLock`: `netstandard2.1`, depends on `SyZero.Core`
- `RedisDistributedLockProvider`: ships with `SyZero.Redis` — no separate NuGet package
- A database-backed implementation (reusing the existing `IServiceManagement` table) is planned.
