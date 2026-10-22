using System;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SyZero.AspNetCore.Middleware
{
    /// <summary>
    /// 全局异常处理中间件：捕获异常 → 输出 RFC 7807 ProblemDetails JSON，附带 TraceId。
    /// 注册：<c>app.UseMiddleware&lt;SyExceptionMiddleware&gt;()</c> 或 <c>app.UseSyZeroExceptionHandler()</c>。
    /// </summary>
    public class SyExceptionMiddleware : IMiddleware
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly ILogger<SyExceptionMiddleware> _logger;

        public SyExceptionMiddleware(ILogger<SyExceptionMiddleware> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            try
            {
                await next(context).ConfigureAwait(false);
            }
            catch (SyBusinessException bex)
            {
                _logger.LogWarning(bex, "SyBusinessException: {Code} {Title}", bex.Error.Code, bex.Error.Title);
                await WriteAsync(context, bex.Error).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception");
                var error = SyError.Internal(detail: ex.Message);
                await WriteAsync(context, error).ConfigureAwait(false);
            }
        }

        private static async Task WriteAsync(HttpContext context, SyError error)
        {
            if (context.Response.HasStarted)
            {
                return;
            }
            var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
            context.Response.Clear();
            context.Response.StatusCode = error.Status;
            context.Response.ContentType = "application/problem+json";

            var payload = new
            {
                type = error.Type ?? $"https://syzero.dev/errors/{error.Code}",
                title = error.Title,
                status = error.Status,
                detail = error.Detail,
                code = error.Code,
                traceId
            };
            var json = JsonSerializer.Serialize(payload, JsonOptions);
            await context.Response.WriteAsync(json).ConfigureAwait(false);
        }
    }
}
