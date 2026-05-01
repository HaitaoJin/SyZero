using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using SyZero.Example2.Core.DbContext;
using SyZero.Example2.Web.Core.Filter;
using SyZero.DynamicWebApi;
using SyZero.Web.Common;

namespace SyZero.Example2.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            var builder = WebApplication.CreateBuilder(args);

            //使用SyZero
            builder.AddSyZero();

            //builder.Configuration.AddNacos(cancellationTokenSource.Token); //Nacos动态配置
            //builder.Configuration.AddConsul(cancellationTokenSource.Token);

            builder.WebHost.UseUrls($"{AppConfig.ServerOptions.Protocol}://*:{AppConfig.ServerOptions.Port}");

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
                DefaultAreaName = AppConfig.ServerOptions.Name
            });
            //Swagger
            builder.Services.AddSwagger();
            //使用OpenTelemetry遥测
            //builder.Services.AddSyZeroOpenTelemetry();
            //使用AutoMapper
            builder.Services.AddSyZeroAutoMapper();
            //使用SqlSugar仓储
            builder.Services.AddSyZeroSqlSugar<Example2DbContext>();
            //注入控制器
            builder.Services.AddSyZeroController();
            //注入Redis
            //builder.Services.AddSyZeroRedis();
            //注入公共层
            builder.Services.AddSyZeroCommon();
            //注入本地服务管理（用于 Feign 服务发现）
            builder.Services.AddLocalServiceManagement();
            //注入Consul
            //builder.Services.AddConsul();
            //注入Feign
            builder.Services.AddSyZeroFeign();

            // ============================================================
            // v1.5.0 集成示例（默认开启 AI Echo；Kafka / Outbox 以注释提供）
            // ============================================================
            // AI 抽象：默认 EchoChatClient 占位，业务通过 ISyChatClient 调用即可
            builder.Services.AddSyZeroAIEcho();
            // 接入真正的大模型时（OpenAI / Azure OpenAI / Ollama 等），改为：
            // builder.Services.AddSyZeroAIChatClient(new OpenAIChatClient(...));

            // Kafka 消息总线示例
            // builder.Services.AddSyZeroKafka(o =>
            // {
            //     o.BootstrapServers = "kafka:9092";
            //     o.GroupId = AppConfig.ServerOptions.Name;
            // });
            // builder.Services.AddHostedService<MyKafkaConsumer>();

            // 分布式事务 Outbox（搭配 SyZero.EventBus.* 使用）
            // builder.Services.AddSyZeroOutbox();
            // 生产环境建议替换为 EF Core 持久化 Outbox：
            // builder.Services.AddSyZeroOutboxEfCore(opt => opt.UseSqlServer(connectionString));

            var app = builder.Build();

            // SyZero.Diagnostics：统一请求诊断中间件（替代手写 Console 日志）
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
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "SyZero.Example2.Web API V1");
                c.RoutePrefix = "api/swagger";

            });
            //app.UseConsul();
            app.InitTables();
            app.Run();
        }
    }
}
