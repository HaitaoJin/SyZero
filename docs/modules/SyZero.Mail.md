# SyZero.Mail

邮件发送抽象 `ISyMailSender` + 基于 `SmtpClient` 的默认实现 + 控制台占位实现。

## 📦 安装

```bash
dotnet add package SyZero.Mail
```

## 🚀 快速开始

```csharp
// 生产环境（SMTP）
builder.Services.AddSyZeroSmtpMail(o =>
{
    o.Host = "smtp.example.com";
    o.Port = 587;
    o.EnableSsl = true;
    o.UserName = "noreply@example.com";
    o.Password = "***";
    o.From = "noreply@example.com";
});

// 开发 / 单测：仅写控制台
builder.Services.AddSyZeroConsoleMail();

public class WelcomeApp(ISyMailSender mail)
{
    public Task NotifyAsync(string to)
        => mail.SendAsync(to, "欢迎", "<h1>Hello SyZero</h1>", isHtml: true);
}
```
