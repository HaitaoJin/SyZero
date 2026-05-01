using System;
using System.IO;
using System.Linq;
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

            var key = headerValues.ToString();
            var ttl = TimeSpan.FromSeconds(_options.ExpirationSeconds);

            var existing = await _store.GetAsync(key, context.RequestAborted).ConfigureAwait(false);
            if (existing != null)
            {
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

            try
            {
                await next(context).ConfigureAwait(false);
            }
            finally
            {
                buffer.Position = 0;
                var bodyBytes = buffer.ToArray();
                await originalBody.WriteAsync(bodyBytes, 0, bodyBytes.Length).ConfigureAwait(false);
                context.Response.Body = originalBody;

                if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 500)
                {
                    var entry = new IdempotencyEntry
                    {
                        StatusCode = context.Response.StatusCode,
                        ContentType = context.Response.ContentType,
                        Body = bodyBytes
                    };
                    await _store.SaveAsync(key, entry, ttl).ConfigureAwait(false);
                }
            }
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
