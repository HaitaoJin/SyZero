# SyZero.Realtime

基于 ASP.NET Core SignalR 的 SyZero 实时推送默认实现。提供 `SyRealtimeHub` 与 `ISyRealtimeNotifier` 抽象，业务侧不直接依赖 `IHubContext`，便于单测与替换。

## 📦 安装

```bash
dotnet add package SyZero.Realtime
```

## 🚀 快速开始

```csharp
builder.Services.AddSyZeroRealtime();

var app = builder.Build();
app.MapSyZeroRealtime(); // 默认映射到 /hubs/sy-realtime

public class OrderApp(ISyRealtimeNotifier notifier)
{
    public Task NotifyPaidAsync(long orderId)
        => notifier.BroadcastAsync("OrderPaid", new { orderId });
}
```

## 🧩 客户端

```ts
const conn = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/sy-realtime")
    .build();
conn.on("OrderPaid", e => console.log("paid:", e.orderId));
await conn.start();
```
