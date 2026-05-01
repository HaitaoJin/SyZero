# SyZero.Mail

`ISyMailSender` abstraction + `SmtpClient`-based default implementation + a console placeholder for dev / tests.

## 📦 Install

```bash
dotnet add package SyZero.Mail
```

## 🚀 Quick Start

```csharp
// Production (SMTP)
builder.Services.AddSyZeroSmtpMail(o =>
{
    o.Host = "smtp.example.com";
    o.Port = 587;
    o.EnableSsl = true;
    o.UserName = "noreply@example.com";
    o.Password = "***";
    o.From = "noreply@example.com";
});

// Dev / tests: console only
builder.Services.AddSyZeroConsoleMail();

public class WelcomeApp(ISyMailSender mail)
{
    public Task NotifyAsync(string to)
        => mail.SendAsync(to, "Welcome", "<h1>Hello SyZero</h1>", isHtml: true);
}
```
