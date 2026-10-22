# SyZero.Audit

> ⚠️ **Merged into [SyZero.Core](./SyZero.Core.md) since v9.1.0**: no need to install `SyZero.Audit` separately — referencing `SyZero.Core` is enough. The namespace is now `SyZero`; just remove `using SyZero.Audit;`.

Audit logging module: provides a unified `AuditLogEntry` model, a pluggable `IAuditLogStore` abstraction, and a `[Sensitive]`-aware data masker for safely capturing "who did what to which resource and when".

## 📦 Installation

Ships with `SyZero.Core`:

```bash
dotnet add package SyZero.Core
```

## ✨ Features

- 📒 Unified `AuditLogEntry`: user, tenant, action, resource, duration, success, extras
- 🔌 Pluggable `IAuditLogStore`: in-memory by default; replace with EF / Mongo / ES
- 🛡️ Field masking via `[Sensitive]` attribute → values become `***`
- 🪶 Zero third-party deps; only depends on `SyZero.Core`

## 🚀 Quick Start

```csharp
builder.Services.AddSyZeroAudit();
builder.Services.AddSingleton<IAuditLogStore, EfAuditLogStore>(); // optional override

public class PayRequest
{
    public string OrderId { get; set; }
    [Sensitive] public string CardNumber { get; set; }
}

var sw = Stopwatch.StartNew();
await store.SaveAsync(new AuditLogEntry
{
    Action = "Order.Pay",
    Resource = "Order",
    ResourceId = req.OrderId,
    DurationMs = sw.ElapsedMilliseconds,
    Extras = { ["payload"] = masker.Serialize(req) },
});
```

## 📜 License

[Apache 2.0](https://github.com/SyZeroOrg/SyZero/blob/main/LICENSE)
