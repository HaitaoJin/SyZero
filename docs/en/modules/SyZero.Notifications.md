# SyZero.Notifications

Unified notification orchestration through `INotificationDispatcher`. A single business notification can target mail, SMS, or custom channels and returns a delivery result for every channel.

```csharp
services.AddSyZeroNotifications();

await dispatcher.SendAsync(new NotificationMessage(
    "user@example.com", "Order created", "Order #1001 was created.", ["mail"]));
```

Implement `INotificationChannel` to add in-app notifications, webhooks, or other providers.
