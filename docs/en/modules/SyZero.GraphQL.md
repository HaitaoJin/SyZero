# SyZero.GraphQL

Minimal wrapper around [HotChocolate](https://chillicream.com/docs/hotchocolate): `AddSyZeroGraphQL` + `MapSyZeroGraphQL`. Ships a default `SyZeroPingQuery` exposing the `ping: String` field.

## 📦 Install

```bash
dotnet add package SyZero.GraphQL
```

## 🚀 Quick start

```csharp
builder.Services.AddSyZeroGraphQL(b =>
{
    b.AddType<MyQuery>();
    b.AddMutationType<MyMutation>();
});

app.UseRouting();
app.MapSyZeroGraphQL();   // default /graphql
```

```graphql
{ ping }
# => { "data": { "ping": "pong" } }
```

For DataLoader / Authorization / advanced features see HotChocolate's official docs.
