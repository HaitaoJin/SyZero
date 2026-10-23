# SyZero.Notifications

统一通知编排模块。通过 `INotificationDispatcher` 将同一业务通知路由到邮件、短信或自定义通道，并逐通道返回投递结果。

```csharp
services.AddSyZeroNotifications();

await dispatcher.SendAsync(new NotificationMessage(
    "user@example.com", "订单已创建", "订单 #1001 已创建。", ["mail"]));
```

实现 `INotificationChannel` 即可添加站内信、Webhook、企业微信等通道。
