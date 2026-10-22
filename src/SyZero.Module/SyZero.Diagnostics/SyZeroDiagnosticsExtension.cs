using Microsoft.AspNetCore.Builder;
using SyZero.Diagnostics;

namespace SyZero;

/// <summary>SyZero.Diagnostics 的中间件扩展。</summary>
public static class SyZeroDiagnosticsExtension
{
    /// <summary>启用 SyZero 请求诊断中间件（建议尽量靠前）。</summary>
    public static IApplicationBuilder UseSyZeroDiagnostics(this IApplicationBuilder app)
        => app.UseMiddleware<SyDiagnosticsMiddleware>();
}
