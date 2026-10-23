# SyZero.Swagger

Native .NET 10 OpenAPI integration for SyZero. Since v10 this package uses `Microsoft.AspNetCore.OpenApi` and no longer depends on Swashbuckle.

```bash
dotnet add package SyZero.Swagger
```

```csharp
using SyZero.Swagger;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddSyZeroOpenApi();

var app = builder.Build();
app.MapControllers();
app.MapSyZeroOpenApi();
app.Run();
```

The default document URL is `/openapi/v1.json`. A custom document name and route pattern can be supplied:

```csharp
builder.Services.AddSyZeroOpenApi("internal");
app.MapSyZeroOpenApi("/docs/{documentName}.json");
```

The module generates a standards-based OpenAPI document and does not bundle a UI. Applications can add Scalar, Swagger UI, or another frontend separately and protect the production document endpoint as needed.

## v10 migration

- `AddSwagger(...)` → `AddSyZeroOpenApi(...)`
- `UseSwagger()` / `UseSwaggerUI()` → `MapSyZeroOpenApi()`; choose a UI in the application
- Legacy XML operation filters, build-time Swagger export, and Swashbuckle-specific options were removed
