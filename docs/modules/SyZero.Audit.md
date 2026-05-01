# SyZero.Audit

审计日志模块：提供审计条目模型、可插拔存储抽象 `IAuditLogStore`，以及基于 `[Sensitive]` 特性的字段脱敏序列化器，便于在业务层记录"谁在何时对哪个资源做了什么"。

## 📦 安装

```bash
dotnet add package SyZero.Audit
```

## ✨ 特性

- 📒 **统一模型** `AuditLogEntry`：用户、租户、动作、资源、耗时、成功/失败、扩展字典
- 🔌 **可插拔存储** `IAuditLogStore`：默认 `MemoryAuditLogStore`，业务可替换为 EF / Mongo / ES 等
- 🛡️ **字段脱敏**：在审计输入对象的属性上加 `[Sensitive]` 即可被自动替换为 `***`
- 🪶 **零依赖**：仅依赖 `SyZero.Core`，可在任意分层使用

## 🚀 快速开始

### 1. 注册

```csharp
builder.Services.AddSyZeroAudit();

// 替换默认内存存储
builder.Services.AddSingleton<IAuditLogStore, EfAuditLogStore>();
```

### 2. 业务侧记录

```csharp
public class OrderService
{
    private readonly IAuditLogStore _audit;
    private readonly IAuditDataMasker _masker;
    public OrderService(IAuditLogStore audit, IAuditDataMasker masker)
    { _audit = audit; _masker = masker; }

    public async Task PayAsync(PayRequest req)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            // ... 业务逻辑 ...
            await _audit.SaveAsync(new AuditLogEntry
            {
                Action = "Order.Pay",
                Resource = "Order",
                ResourceId = req.OrderId,
                DurationMs = sw.ElapsedMilliseconds,
                Extras = { ["payload"] = _masker.Serialize(req) },
            });
        }
        catch (Exception ex)
        {
            await _audit.SaveAsync(new AuditLogEntry
            {
                Action = "Order.Pay",
                Success = false,
                ExceptionMessage = ex.Message,
                DurationMs = sw.ElapsedMilliseconds,
            });
            throw;
        }
    }
}

public class PayRequest
{
    public string OrderId { get; set; }
    [Sensitive] public string CardNumber { get; set; }
}
```

## 📜 许可证

[Apache 2.0](https://github.com/SyZeroOrg/SyZero/blob/main/LICENSE)
