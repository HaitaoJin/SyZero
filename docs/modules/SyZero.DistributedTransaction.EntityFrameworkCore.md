# SyZero.DistributedTransaction.EntityFrameworkCore

为 `SyZero.DistributedTransaction` 提供 **EF Core 持久化 Outbox** 实现，替换默认的 `MemoryOutboxStore`。

## 📦 安装

```bash
dotnet add package SyZero.DistributedTransaction.EntityFrameworkCore
```

## 🚀 快速开始

```csharp
builder.Services.AddSyZeroOutbox();              // 来自 SyZero.DistributedTransaction
builder.Services.AddSyZeroOutboxEfCore(opt =>    // 替换 IOutboxStore
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
```

启动时执行迁移（或 `EnsureCreated`）以创建 `SyOutboxMessages` 表。

## 🗂 表结构

| 列 | 说明 |
| --- | --- |
| `Id` (PK, string 64) | 消息 Id（hex GUID） |
| `EventName` | 事件名 |
| `PayloadJson` | 序列化 payload |
| `CreatedAtUtc` | 入队时间（UTC） |
| `PublishedAtUtc` | 发布完成时间（含索引） |
| `RetryCount` | 重试次数 |
| `LastError` | 最近错误 |

## 🔄 替换关系

`AddSyZeroOutboxEfCore` 通过 `services.Replace(...)` 把 `IOutboxStore` 从默认的 `MemoryOutboxStore` 切换到 `EfCoreOutboxStore`。
