# SyZero.Core

SyZero.Core is the lightweight core package of the framework. It provides dependency injection conventions, application service infrastructure, repository and unit-of-work abstractions, lightweight service management, and lightweight event bus implementations.

## Install

```bash
dotnet add package SyZero.Core
```

## Key Features

- Convention-based dependency injection
- ApplicationService and SyZeroServiceBase infrastructure
- Repository and unit-of-work abstractions
- Local and DB-based service management
- Local and DB-based event bus
- Dynamic API provider abstractions in `SyZero.Application.DynamicApi` (`DynamicApiTypeFilter`, `DynamicApiNameNormalizer`, `DynamicApiAttributeCache`, `DynamicApiAssemblyScanner`, `DynamicApiOptionsBase`, ...) shared by [`SyZero.DynamicWebApi`](./SyZero.DynamicWebApi) and [`SyZero.DynamicGrpc`](./SyZero.DynamicGrpc), enabling additional providers to be added in parallel

## Quick Start

```csharp
using SyZero;

var builder = WebApplication.CreateBuilder(args);
builder.AddSyZero();

var app = builder.Build();
app.UseSyZero();
app.Run();
```

## Notes

- Use this package as the base of all SyZero applications.
- Add additional SyZero modules only when you need their capabilities.

## References

- Chinese documentation: [/modules/SyZero.Core](/modules/SyZero.Core)


