using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading.Tasks;

namespace SyZero.Diagnostics;

/// <summary>
/// 统一请求诊断中间件：写入结构化日志（method / path / status / elapsed / TraceId / UserId / TenantId），
/// 并将这些字段填入 <see cref="HttpContext.Items"/> 便于业务后续读取。
/// </summary>
public sealed class SyDiagnosticsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SyDiagnosticsMiddleware> _logger;

    public SyDiagnosticsMiddleware(RequestDelegate next, ILogger<SyDiagnosticsMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        var traceId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;

        context.Items["sy.TraceId"] = traceId;
        context.Items["sy.UserId"] = context.User?.Identity?.Name;
        context.Items["sy.TenantId"] = context.Request.Headers.TryGetValue("X-Tenant-Id", out var t) ? t.ToString() : null;

        try
        {
            await _next(context).ConfigureAwait(false);
        }
        finally
        {
            sw.Stop();
            _logger.LogInformation(
                "{Method} {Path} -> {Status} in {Elapsed}ms TraceId={TraceId} UserId={UserId} TenantId={TenantId}",
                context.Request.Method,
                context.Request.Path.Value,
                context.Response.StatusCode,
                sw.ElapsedMilliseconds,
                traceId,
                context.Items["sy.UserId"],
                context.Items["sy.TenantId"]);
        }
    }
}
