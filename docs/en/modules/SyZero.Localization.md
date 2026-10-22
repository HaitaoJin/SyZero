# SyZero.Localization

Wraps `Microsoft.Extensions.Localization` behind a SyZero-style `ISyStringLocalizer`.

## 📦 Install

```bash
dotnet add package SyZero.Localization
```

## 🚀 Quick Start

```csharp
builder.Services.AddSyZeroLocalization("Resources");

// Resources/SharedResource.zh-CN.resx -> key: Hello, value: 你好
// Resources/SharedResource.en.resx    -> key: Hello, value: Hello

public class GreetingService(ISyStringLocalizer loc)
{
    public string SayHello() => loc["Hello"];
}
```

Missing keys fall back to the key itself (consistent with `Microsoft.Extensions.Localization`). Use `app.UseRequestLocalization(...)` to configure supported cultures.
