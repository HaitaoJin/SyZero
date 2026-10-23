using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace SyZero.Idempotency
{
    /// <summary>
    /// 幂等性中间件：根据 <c>Idempotency-Key</c> 头复用之前的响应。
    /// </summary>
    public class IdempotencyMiddleware : IMiddleware
    {
        private readonly IIdempotencyStore _store;
        private readonly IdempotencyOptions _options;

        public IdempotencyMiddleware(IIdempotencyStore store, IOptions<IdempotencyOptions>? options = null)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _options = options?.Value ?? new IdempotencyOptions();
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (!_options.Methods.Contains(context.Request.Method, StringComparer.OrdinalIgnoreCase))
            {
                await next(context).ConfigureAwait(false);
                return;
            }

            if (!context.Request.Headers.TryGetValue(_options.HeaderName, out var headerValues)
                || string.IsNullOrWhiteSpace(headerValues.ToString()))
            {
                await next(context).ConfigureAwait(false);
                return;
            }

            var clientKey = headerValues.ToString();
            var requestHash = await ComputeRequestHashAsync(context).ConfigureAwait(false);
            var key = BuildStorageKey(context, clientKey);
            var ttl = TimeSpan.FromSeconds(_options.ExpirationSeconds);

            var existing = await _store.GetAsync(key, context.RequestAborted).ConfigureAwait(false);
            if (existing != null)
            {
                if (!string.Equals(existing.RequestHash, requestHash, StringComparison.Ordinal))
                {
                    context.Response.StatusCode = _options.ConflictStatusCode;
                    await context.Response.WriteAsync("Idempotency key was already used for a different request", context.RequestAborted).ConfigureAwait(false);
                    return;
                }

                await WriteAsync(context, existing).ConfigureAwait(false);
                return;
            }

            var reserved = await _store.TryReserveAsync(key, ttl, context.RequestAborted).ConfigureAwait(false);
            if (!reserved)
            {
                // 已占位但尚无响应：拒绝避免重复执行
                context.Response.StatusCode = _options.InflightRejectionStatusCode;
                await context.Response.WriteAsync("Duplicate request in progress").ConfigureAwait(false);
                return;
            }

            var originalBody = context.Response.Body;
            using var buffer = new MemoryStream();
            context.Response.Body = buffer;

            var completed = false;
            try
            {
                await next(context).ConfigureAwait(false);
                completed = true;
            }
            finally
            {
                buffer.Position = 0;
                var bodyBytes = buffer.ToArray();
                await originalBody.WriteAsync(bodyBytes, 0, bodyBytes.Length).ConfigureAwait(false);
                context.Response.Body = originalBody;

                if (completed
                    && context.Response.StatusCode >= 200
                    && context.Response.StatusCode < 500
                    && bodyBytes.Length <= _options.MaxResponseBodyBytes)
                {
                    var entry = new IdempotencyEntry
                    {
                        StatusCode = context.Response.StatusCode,
                        ContentType = context.Response.ContentType,
                        Body = bodyBytes,
                        RequestHash = requestHash
                    };
                    await _store.SaveAsync(key, entry, ttl, context.RequestAborted).ConfigureAwait(false);
                }
                else
                {
                    await _store.ReleaseAsync(key, CancellationToken.None).ConfigureAwait(false);
                }
            }
        }

        private string BuildStorageKey(HttpContext context, string clientKey)
        {
            if (!_options.ScopeKeyToRequest)
            {
                return clientKey;
            }

            var tenant = context.Items["sy.TenantId"]?.ToString()
                ?? context.User.FindFirst("tenant_id")?.Value
                ?? "-";
            var user = context.User.Identity?.Name ?? "anonymous";
            var raw = string.Join('|', tenant, user, context.Request.Method, context.Request.Path.Value, clientKey);
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
        }

        private async Task<string> ComputeRequestHashAsync(HttpContext context)
        {
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            var prefix = Encoding.UTF8.GetBytes($"{context.Request.Method}\n{context.Request.Path}\n{context.Request.QueryString}\n");
            hash.AppendData(prefix);

            if (context.Request.ContentLength is > 0)
            {
                if (context.Request.ContentLength > _options.MaxRequestBodyBytes)
                {
                    throw new BadHttpRequestException($"请求体超过幂等指纹上限 {_options.MaxRequestBodyBytes} 字节。", StatusCodes.Status413PayloadTooLarge);
                }

                context.Request.EnableBuffering();
                var buffer = new byte[Math.Min(81920, _options.MaxRequestBodyBytes)];
                var total = 0;
                int read;
                while ((read = await context.Request.Body.ReadAsync(buffer.AsMemory(0, buffer.Length), context.RequestAborted).ConfigureAwait(false)) > 0)
                {
                    total += read;
                    if (total > _options.MaxRequestBodyBytes)
                    {
                        context.Request.Body.Position = 0;
                        throw new BadHttpRequestException($"请求体超过幂等指纹上限 {_options.MaxRequestBodyBytes} 字节。", StatusCodes.Status413PayloadTooLarge);
                    }
                    hash.AppendData(buffer, 0, read);
                }
                context.Request.Body.Position = 0;
            }

            return Convert.ToHexString(hash.GetHashAndReset());
        }

        private static async Task WriteAsync(HttpContext context, IdempotencyEntry entry)
        {
            context.Response.StatusCode = entry.StatusCode;
            if (!string.IsNullOrEmpty(entry.ContentType))
            {
                context.Response.ContentType = entry.ContentType;
            }
            if (entry.Body != null && entry.Body.Length > 0)
            {
                await context.Response.Body.WriteAsync(entry.Body, 0, entry.Body.Length).ConfigureAwait(false);
            }
        }
    }
}
