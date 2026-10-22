# SyZero.Realtime

ASP.NET Core SignalR-based default for SyZero realtime push. Provides `SyRealtimeHub` + `ISyRealtimeNotifier` so business code does not depend on `IHubContext` directly.

## 📦 Install

```bash
dotnet add package SyZero.Realtime
```

## 🚀 Quick Start

```csharp
builder.Services.AddSyZeroRealtime();

var app = builder.Build();
app.MapSyZeroRealtime(); // defaults to /hubs/sy-realtime

public class OrderApp(ISyRealtimeNotifier notifier)
{
    public Task NotifyPaidAsync(long orderId)
        => notifier.BroadcastAsync("OrderPaid", new { orderId });
}
```

Client (TypeScript):

```ts
const conn = new signalR.HubConnectionBuilder().withUrl("/hubs/sy-realtime").build();
conn.on("OrderPaid", e => console.log("paid:", e.orderId));
await conn.start();
```
