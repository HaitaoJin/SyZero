using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SyZero.HealthChecks;

namespace SyZero
{
    /// <summary>
    /// SyZero.HealthChecks 扩展方法。
    /// </summary>
    public static class SyZeroHealthChecksExtension
    {
        /// <summary>
        /// 标识"就绪"检查（依赖外部资源），用于 /health/ready 端点筛选。
        /// </summary>
        public const string ReadyTag = "ready";

        /// <summary>
        /// 标识"存活"检查（仅进程级），用于 /health/live 端点筛选。
        /// </summary>
        public const string LiveTag = "live";

        /// <summary>
        /// 注册 SyZero 健康检查。返回 <see cref="IHealthChecksBuilder"/> 供链式注册具体检查器。
        /// </summary>
        public static IHealthChecksBuilder AddSyZeroHealthChecks(
            this IServiceCollection services,
            IConfiguration configuration,
            Action<SyZeroHealthChecksOptions>? configure = null,
            string sectionName = SyZeroHealthChecksOptions.SectionName)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            var options = new SyZeroHealthChecksOptions();
            configuration.GetSection(sectionName).Bind(options);
            configure?.Invoke(options);
            services.AddSingleton(options);

            return services.AddHealthChecks();
        }

        /// <summary>
        /// 映射健康检查端点：/health/live、/health/ready、/health。
        /// </summary>
        public static IEndpointRouteBuilder UseSyZeroHealthChecks(this IEndpointRouteBuilder endpoints)
        {
            var options = endpoints.ServiceProvider.GetService<SyZeroHealthChecksOptions>()
                ?? new SyZeroHealthChecksOptions();

            var responseWriter = options.DetailedResponse
                ? (Func<HttpContext, HealthReport, Task>)WriteDetailedResponse
                : null!;

            // /health/live —— 仅 live 标签
            var liveOpts = new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains(LiveTag) || check.Tags.Count == 0,
            };
            if (responseWriter != null) liveOpts.ResponseWriter = responseWriter;
            endpoints.MapHealthChecks(options.LivePath, liveOpts);

            // /health/ready —— 仅 ready 标签
            var readyOpts = new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains(ReadyTag),
            };
            if (responseWriter != null) readyOpts.ResponseWriter = responseWriter;
            endpoints.MapHealthChecks(options.ReadyPath, readyOpts);

            // /health —— 全部
            var overallOpts = new HealthCheckOptions();
            if (responseWriter != null) overallOpts.ResponseWriter = responseWriter;
            endpoints.MapHealthChecks(options.OverallPath, overallOpts);

            return endpoints;
        }

        private static async Task WriteDetailedResponse(HttpContext context, HealthReport report)
        {
            context.Response.ContentType = "application/json; charset=utf-8";
            var payload = new
            {
                status = report.Status.ToString(),
                totalDurationMs = report.TotalDuration.TotalMilliseconds,
                entries = report.Entries.Select(kv => new
                {
                    name = kv.Key,
                    status = kv.Value.Status.ToString(),
                    durationMs = kv.Value.Duration.TotalMilliseconds,
                    description = kv.Value.Description,
                    tags = kv.Value.Tags,
                    exception = kv.Value.Exception?.Message,
                }),
            };
            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }
    }
}
