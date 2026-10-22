# SyZero.Idempotency

ASP.NET Core idempotency middleware + storage abstraction. Replays a previous response when the same `Idempotency-Key` header is observed within the configured window.

## 📦 Install

```bash
dotnet add package SyZero.Idempotency
```

## ✨ Features

- 🛡️ **`Idempotency-Key` header** — industry convention (Stripe / GitHub / RFC draft).
- 🔄 **Response replay** — first successful response is cached in `IIdempotencyStore`; subsequent calls return it verbatim.
- ⚡ **Inflight reservation** — first request reserves the slot atomically; concurrent duplicates are rejected with 409.
- 🔌 **Pluggable store** — default `MemoryIdempotencyStore` (single instance / tests); swap for Redis or SQL in production.
- 🎯 **Method filter** — POST / PUT / PATCH / DELETE by default; GET / HEAD treated as inherently idempotent.

## 🚀 Quick Start

```csharp
builder.Services.AddSyZeroIdempotency(builder.Configuration);

app.UseRouting();
app.UseSyZeroIdempotency();
app.MapControllers();
```

```http
POST /api/orders
Idempotency-Key: 5d1a3f08-...-uuid
```

## ⚙️ Options

| Field | Default | Notes |
|---|---|---|
| `HeaderName` | `Idempotency-Key` | Header carrying the key |
| `ExpirationSeconds` | 86400 | Key TTL |
| `InflightRejectionStatusCode` | 409 | Status when first request still in-flight |
| `Methods` | POST/PUT/PATCH/DELETE | Methods to intercept |

## ⚠️ Notes

- Default `MemoryIdempotencyStore` is **single-instance only**; swap for Redis / SQL in multi-instance setups.
- The middleware **buffers the entire response body**; avoid for very large payloads.
- Only 2xx / 4xx responses are cached; 5xx are not, so retries can succeed.
