using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SyZero.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace SyZero.Tests;

public class DiagnosticsMiddlewareTests
{
    [Fact]
    public async Task Middleware_PopulatesContextItemsAndCallsNext()
    {
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new MemoryStream();
        ctx.Request.Method = "GET";
        ctx.Request.Path = "/x";
        ctx.Request.Headers["X-Tenant-Id"] = "t-001";

        var called = false;
        RequestDelegate next = c =>
        {
            called = true;
            c.Response.StatusCode = 200;
            return Task.CompletedTask;
        };

        var mw = new SyDiagnosticsMiddleware(next, NullLogger<SyDiagnosticsMiddleware>.Instance);
        await mw.InvokeAsync(ctx);

        Assert.True(called);
        Assert.NotNull(ctx.Items["sy.TraceId"]);
        Assert.Equal("t-001", ctx.Items["sy.TenantId"]);
    }
}
