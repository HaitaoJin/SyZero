# SyZero.DistributedTransaction.EntityFrameworkCore

EF Core-backed **persistent Outbox store** that replaces the default `MemoryOutboxStore` from `SyZero.DistributedTransaction`.

## 📦 Install

```bash
dotnet add package SyZero.DistributedTransaction.EntityFrameworkCore
```

## 🚀 Quick start

```csharp
builder.Services.AddSyZeroOutbox();
builder.Services.AddSyZeroOutboxEfCore(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
```

Run migrations (or `EnsureCreated`) so that the `SyOutboxMessages` table is created.

## 🗂 Schema

| Column | Description |
| --- | --- |
| `Id` (PK, string 64) | Hex GUID |
| `EventName` | Event name |
| `PayloadJson` | Serialised payload |
| `CreatedAtUtc` | UTC enqueue time |
| `PublishedAtUtc` | UTC publish time (indexed) |
| `RetryCount` | Retry counter |
| `LastError` | Last error message |
