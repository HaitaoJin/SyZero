# SyZero.Localization

包装 `Microsoft.Extensions.Localization`，提供 SyZero 风格的 `ISyStringLocalizer` 抽象。

## 📦 安装

```bash
dotnet add package SyZero.Localization
```

## ✨ 特性

- 🌐 **`ISyStringLocalizer["key"]`**：业务统一通过此接口取多语言资源，便于替换实现 / mock。
- 📁 **资源约定**：默认从 `Resources/SharedResource.{culture}.resx` 加载。
- 🔌 **`AddSyZeroLocalization(resourcesPath, configure?)`**：一行注册。

## 🚀 快速开始

```csharp
builder.Services.AddSyZeroLocalization("Resources");

// Resources/SharedResource.zh-CN.resx
//   key=Hello, value=你好
// Resources/SharedResource.en.resx
//   key=Hello, value=Hello

public class GreetingService
{
    private readonly ISyStringLocalizer _loc;
    public GreetingService(ISyStringLocalizer loc) => _loc = loc;
    public string SayHello() => _loc["Hello"];
}
```

## 🧩 注意

- 资源缺失时返回 key 本身（与 Microsoft.Extensions.Localization 行为一致）。
- 当前文化（`CultureInfo.CurrentUICulture`）由 ASP.NET Core 请求本地化中间件设置；调用 `app.UseRequestLocalization(...)` 配置支持的 culture 列表。
