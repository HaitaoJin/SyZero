# SyZero.Sms

短信发送抽象 `ISySmsSender` + 控制台占位实现。具体通道（阿里云、腾讯云、Twilio 等）建议作为派生包按需接入。

## 📦 安装

```bash
dotnet add package SyZero.Sms
```

## 🚀 快速开始

```csharp
// 开发 / 单测：仅写控制台
builder.Services.AddSyZeroConsoleSms();

public class CodeApp(ISySmsSender sms)
{
    public Task SendCodeAsync(string phone, string code)
        => sms.SendAsync(phone, "SMS_LOGIN_CODE", new { code });
}
```

## 🧩 自定义通道

实现 `ISySmsSender` 后通过 `services.AddSingleton<ISySmsSender, MyAliyunSmsSender>()` 替换默认实现即可。
