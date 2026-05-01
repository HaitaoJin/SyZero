# SyZero.Validation

Validation framework: `ISyValidator<T>` abstraction + `ISyValidatorRunner` entry point with a `DataAnnotations`-based fallback. Easy to adapt FluentValidation or any other library.

## 📦 Installation

```bash
dotnet add package SyZero.Validation
```

## ✨ Features

- 🧪 Unified `ISyValidator<T>` + `SyValidationResult`
- 🪂 DataAnnotations fallback — works without writing any validator
- 🔌 Pluggable: register your own validator per DTO type
- 🪶 No third-party deps; only `SyZero.Core`

## 🚀 Quick Start

```csharp
builder.Services.AddSyZeroValidation();
builder.Services.AddTransient<ISyValidator<CreateOrderDto>, CreateOrderValidator>();

public class OrderService(ISyValidatorRunner runner)
{
    public async Task CreateAsync(CreateOrderDto dto)
    {
        var result = await runner.ValidateAsync(dto);
        if (!result.IsValid)
            throw new BusinessException(string.Join("; ",
                result.Errors.Select(e => $"{e.Field}: {e.Message}")));
    }
}

public class CreateOrderDto
{
    [Required] public string CustomerId { get; set; }
    [Range(1, int.MaxValue)] public int Quantity { get; set; }
}

public class CreateOrderValidator : ISyValidator<CreateOrderDto>
{
    public Task<SyValidationResult> ValidateAsync(CreateOrderDto dto, CancellationToken ct = default)
        => Task.FromResult(dto.Quantity > 1000
            ? SyValidationResult.Invalid(nameof(dto.Quantity), "Max 1000 per order")
            : SyValidationResult.Valid);
}
```

## 📜 License

[Apache 2.0](https://github.com/SyZeroOrg/SyZero/blob/main/LICENSE)
