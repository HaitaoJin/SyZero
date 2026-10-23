# SyZero.Swagger

SyZero 的 .NET 10 原生 OpenAPI 集成。v10 起本包基于 `Microsoft.AspNetCore.OpenApi`，不再依赖 Swashbuckle。

## 安装

```bash
dotnet add package SyZero.Swagger
```

## 快速开始

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

默认文档地址为 `/openapi/v1.json`。可指定文档名和映射模式：

```csharp
builder.Services.AddSyZeroOpenApi("internal");
app.MapSyZeroOpenApi("/docs/{documentName}.json");
```

本模块只负责生成标准 OpenAPI 文档，不内置 UI。需要交互式文档时，可由应用单独选择 Scalar、Swagger UI 或其他前端，并对生产环境的文档端点实施认证或网络访问控制。

## v10 迁移

- `AddSwagger(...)` → `AddSyZeroOpenApi(...)`
- `UseSwagger()` / `UseSwaggerUI()` → `MapSyZeroOpenApi()`，UI 由应用自行提供
- 删除了旧版 XML Operation Filter、构建期 Swagger JSON 导出和 Swashbuckle 专属配置

## 许可证

MIT License - 详见 [LICENSE](../../LICENSE)
