# SyZero.FeatureManagement

包装 [Microsoft.FeatureManagement](https://github.com/microsoft/FeatureManagement-Dotnet)，提供 SyZero 风格的 `ISyFeatureManager` 抽象，便于业务层屏蔽具体实现并易于 mock。

## 📦 安装

```bash
dotnet add package SyZero.FeatureManagement
```

## ✨ 特性

- 🧪 **`ISyFeatureManager.IsEnabledAsync(feature)`**：业务直接依赖此接口，与底层解耦。
- ⚙️ **`appsettings.json` 驱动**：默认从 `FeatureManagement` 节读取布尔开关 / 复杂筛选器。
- 🔌 **完整复用 Microsoft.FeatureManagement**：可直接使用 `[FeatureGate("...")]` 等高级特性。

## 🚀 快速开始

```json
{
  "FeatureManagement": {
    "NewCheckout": true,
    "BetaUI": false
  }
}
```

```csharp
builder.Services.AddSyZeroFeatureManagement(builder.Configuration);

public class CheckoutService
{
    private readonly ISyFeatureManager _features;
    public CheckoutService(ISyFeatureManager features) => _features = features;

    public async Task PayAsync()
    {
        if (await _features.IsEnabledAsync("NewCheckout"))
        {
            // 新版结账流程
        }
    }
}
```
