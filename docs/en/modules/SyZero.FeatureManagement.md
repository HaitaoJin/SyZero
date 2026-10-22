# SyZero.FeatureManagement

Wraps [Microsoft.FeatureManagement](https://github.com/microsoft/FeatureManagement-Dotnet) behind a SyZero-style `ISyFeatureManager` so business code stays decoupled from the underlying provider.

## 📦 Install

```bash
dotnet add package SyZero.FeatureManagement
```

## 🚀 Quick Start

```json
{ "FeatureManagement": { "NewCheckout": true, "BetaUI": false } }
```

```csharp
builder.Services.AddSyZeroFeatureManagement(builder.Configuration);

public class CheckoutService(ISyFeatureManager features)
{
    public async Task PayAsync()
    {
        if (await features.IsEnabledAsync("NewCheckout"))
        {
            // new checkout flow
        }
    }
}
```

You can still use Microsoft.FeatureManagement features such as `[FeatureGate("...")]`.
