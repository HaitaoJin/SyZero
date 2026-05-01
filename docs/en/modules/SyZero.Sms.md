# SyZero.Sms

`ISySmsSender` abstraction + console placeholder. Concrete channels (Aliyun, Tencent Cloud, Twilio, ...) should be shipped as derived packages.

## 📦 Install

```bash
dotnet add package SyZero.Sms
```

## 🚀 Quick Start

```csharp
builder.Services.AddSyZeroConsoleSms();

public class CodeApp(ISySmsSender sms)
{
    public Task SendCodeAsync(string phone, string code)
        => sms.SendAsync(phone, "SMS_LOGIN_CODE", new { code });
}
```

Replace the default by registering your own `ISySmsSender` implementation.
