using Microsoft.AspNetCore.Builder;
using SyZero.Swagger;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using SyZero.Example1.Core.DbContext;
using SyZero.Example1.Application.Examples;
using SyZero.Example1.Web.Core.Filter;
using SyZero.DynamicWebApi;
using SyZero.Web.Common;

namespace SyZero.Example1.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            var builder = WebApplication.CreateBuilder(args);

            //使用SyZero
            builder.AddSyZero();
            var serverOptions = builder.Configuration.GetRequiredSection("SyZero").Get<global::SyZero.Configurations.SyZeroServerOptions>()
                ?? throw new InvalidOperationException("缺少 SyZero 配置节。");

            //builder.Configuration.AddNacos(cancellationTokenSource.Token); //Nacos动态配置
            //builder.Configuration.AddConsul(cancellationTokenSource.Token); //Consul动态配置

            builder.Logging.AddOpenTelemetry(logging =>
            {
                logging.IncludeFormattedMessage = true;
                logging.IncludeScopes = true;
            }).AddSyZeroLog4Net();

            builder.Services.Replace(ServiceDescriptor.Transient<IControllerActivator, ServiceBasedControllerActivator>());

            builder.Services.AddControllers().AddMvcOptions(options =>
            {
                options.Filters.Add(new AppExceptionFilter());
                options.Filters.Add(new AppResultFilter());
            }).AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.Converters.Add(new LongToStrConverter());
            });

            //动态WebApi
            builder.Services.AddDynamicWebApi(new DynamicWebApiOptions()
            {
                DefaultApiPrefix = "/api",
                DefaultAreaName = serverOptions.Name
            }, typeof(Example1AppService).Assembly);
            //Swagger
            builder.Services.AddSyZeroOpenApi();
            //使用OpenTelemetry遥测
            builder.Services.AddSyZeroOpenTelemetry(builder.Configuration);
            //使用AutoMapper
            builder.Services.AddSyZeroAutoMapper();
            //使用SqlSugar仓储
            builder.Services.AddSyZeroSqlSugar<Example1DbContext>(builder.Configuration);
            //注入控制器
            builder.Services.AddSyZeroController();
            //注入公共层
            builder.Services.AddSyZeroCommon(builder.Configuration);
            //注入本地服务管理（用于 Feign 服务发现）
            builder.Services.AddLocalServiceManagement(builder.Configuration);
            //注入Consul
            //builder.Services.AddConsul(builder.Configuration);
            //注入Feign
            builder.Services.AddSyZeroFeign(builder.Configuration);

            // ============================================================
            // v1.5.0 集成示例（默认开启 GraphQL；其余以注释提供）
            // ============================================================
            // GraphQL 端点：默认提供 SyZeroPingQuery（可访问 POST /graphql 查询 { ping }）
            builder.Services.AddSyZeroGraphQL();

            // API Key 认证示例：调用方在请求头携带 X-Api-Key
            // builder.Services.AddAuthentication(SyZero.Authentication.ApiKey.ApiKeyOptions.Scheme)
            //     .AddSyZeroApiKey(o =>
            //     {
            //         o.HeaderName = "X-Api-Key";
            //         o.Keys["sk-demo-12345"] = "demo-client";
            //     });
            // builder.Services.AddAuthorization();

            // SourceGenerator：编译期 DI 注册（取代运行时反射扫描）
            // 业务类型只需实现 IScopedDependency / ISingletonDependency / ITransientDependency 即可被自动收集，
            // 编译后调用以下扩展即可一次性注册到容器：
            // builder.Services.AddSyZeroGenerated();

            var app = builder.Build();

            // SyZero.Diagnostics：统一请求诊断中间件（替代手写 Console 日志）
            // 写入结构化日志：method / path / status / 耗时 / TraceId / UserId / TenantId
            app.UseSyZeroDiagnostics();

            app.UseSyZero();
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseCors(builder =>
            {
                builder.AllowAnyMethod()
                    .SetIsOriginAllowed(_ => true)
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
            app.UseRouting();
            app.UseStaticFiles();
            app.UseSyAuthMiddleware((sySeesion) => "Token:" + sySeesion.UserId);
            app.MapControllers();
            // GraphQL 端点（默认 /graphql）
            app.MapSyZeroGraphQL();
            app.MapSyZeroOpenApi();
            app.InitTables();
            app.Run();
        }
    }
}
