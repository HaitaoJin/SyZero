# SyZero.Validation

校验框架：定义 `ISyValidator<T>` 抽象与 `ISyValidatorRunner` 入口，默认基于 `System.ComponentModel.DataAnnotations` 提供兜底实现，业务可注册任意 `ISyValidator<T>` 适配 FluentValidation 等库。

## 📦 安装

```bash
dotnet add package SyZero.Validation
```

## ✨ 特性

- 🧪 **统一抽象** `ISyValidator<T>` + `SyValidationResult`
- 🪂 **DataAnnotations 兜底**：未自定义校验器时自动回退
- 🔌 **可适配** FluentValidation、自写规则
- 🪶 **零三方依赖**：仅依赖 `SyZero.Core`

## 🚀 快速开始

### 1. 注册

```csharp
builder.Services.AddSyZeroValidation();

// 注册自定义校验器
builder.Services.AddTransient<ISyValidator<CreateOrderDto>, CreateOrderValidator>();
```

### 2. 业务侧使用

```csharp
public class OrderService
{
    private readonly ISyValidatorRunner _runner;
    public OrderService(ISyValidatorRunner runner) => _runner = runner;

    public async Task CreateAsync(CreateOrderDto dto)
    {
        var result = await _runner.ValidateAsync(dto);
        if (!result.IsValid)
        {
            throw new BusinessException(string.Join("; ",
                result.Errors.Select(e => $"{e.Field}: {e.Message}")));
        }
        // ... 业务 ...
    }
}

public class CreateOrderDto
{
    [Required] public string CustomerId { get; set; }
    [Range(1, int.MaxValue)] public int Quantity { get; set; }
}
```

### 3. 自定义校验器示例

```csharp
public class CreateOrderValidator : ISyValidator<CreateOrderDto>
{
    public Task<SyValidationResult> ValidateAsync(CreateOrderDto dto, CancellationToken ct = default)
    {
        if (dto.Quantity > 1000)
            return Task.FromResult(SyValidationResult.Invalid(nameof(dto.Quantity), "单次下单数量不能超过 1000"));
        return Task.FromResult(SyValidationResult.Valid);
    }
}
```

## 📜 许可证

[Apache 2.0](https://github.com/SyZeroOrg/SyZero/blob/main/LICENSE)
