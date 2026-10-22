# SyZero.Authentication.ApiKey

Minimal API Key authentication scheme that maps a request header value to a subject name.

## 📦 Install

```bash
dotnet add package SyZero.Authentication.ApiKey
```

## 🚀 Quick start

```csharp
builder.Services
    .AddAuthentication(ApiKeyOptions.Scheme)
    .AddSyZeroApiKey(o =>
    {
        o.HeaderName = "X-Api-Key";
        o.Keys["sk_live_xxx"] = "service-account-1";
    });

app.UseAuthentication();
app.UseAuthorization();
```

| Scenario | Result |
| --- | --- |
| Missing/blank header | `AuthenticateResult.NoResult()` |
| Header present, key not allow-listed | `AuthenticateResult.Fail("invalid api key")` |
| Key matches | Success, `Identity.Name = Keys[key]` |

Use this scheme for service-to-service or controlled integrations; for end users prefer JWT/OIDC.
