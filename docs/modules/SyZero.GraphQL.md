# SyZero.GraphQL

[HotChocolate](https://chillicream.com/docs/hotchocolate) 的最小封装：`AddSyZeroGraphQL` + `MapSyZeroGraphQL`。默认注册占位 `SyZeroPingQuery` 暴露 `ping: String` 字段。

## 📦 安装

```bash
dotnet add package SyZero.GraphQL
```

## 🚀 快速开始

```csharp
builder.Services.AddSyZeroGraphQL(b =>
{
    b.AddType<MyQuery>();
    b.AddMutationType<MyMutation>();
});

app.UseRouting();
app.MapSyZeroGraphQL();   // 默认 /graphql
```

```graphql
{ ping }
# => { "data": { "ping": "pong" } }
```

## 🔗 进阶

- 类型扩展 / Authorization / DataLoader 等参考 HotChocolate 官方文档。
- 与 `SyZero.Authentication.ApiKey` / JWT 组合：在 `app.MapSyZeroGraphQL().RequireAuthorization(...)` 上加策略。
