using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using SyZero.DynamicWebApi;
using SyZero.Example3.Core.DbContext;
using SyZero.Example3.Web.Core.Filter;
using SyZero.Web.Common;

namespace SyZero.Example3.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            //使用 SyZero
            builder.AddSyZero();

            builder.WebHost.UseUrls($"{AppConfig.ServerOptions.Protocol}://*:{AppConfig.ServerOptions.Port}");

            builder.Logging.AddSyZeroLog4Net();

            builder.Services.Replace(ServiceDescriptor.Transient<IControllerActivator, ServiceBasedControllerActivator>());

            builder.Services.AddControllers().AddMvcOptions(options =>
            {
                options.Filters.Add(new AppExceptionFilter());
                options.Filters.Add(new AppResultFilter());
            }).AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.Converters.Add(new LongToStrConverter());
            });

            //CORS（开发期允许前端 Vite 服务器跨域）
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("Frontend", policy =>
                    policy.SetIsOriginAllowed(_ => true)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials());
            });

            //动态 WebApi
            builder.Services.AddDynamicWebApi(new DynamicWebApiOptions()
            {
                DefaultApiPrefix = "/api",
                DefaultAreaName = AppConfig.ServerOptions.Name
            });
            //Swagger
            builder.Services.AddSwagger();
            //AutoMapper
            builder.Services.AddSyZeroAutoMapper();
            //SqlSugar 仓储
            builder.Services.AddSyZeroSqlSugar<Example3DbContext>();
            //注入控制器
            builder.Services.AddSyZeroController();
            //注入公共层
            builder.Services.AddSyZeroCommon();

            var app = builder.Build();

            // 请求 URL 日志中间件
            app.Use(async (context, next) =>
            {
                var request = context.Request;
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {request.Method} {request.Scheme}://{request.Host}{request.Path}{request.QueryString}");
                await next();
            });

            app.UseSyZero();

            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors("Frontend");
            app.UseRouting();
            app.UseStaticFiles();
            app.UseSyAuthMiddleware((sySeesion) => "Token:" + sySeesion.UserId);
            app.MapControllers();

            // SPA 回退：除 /api 与 /swagger 外的请求回退到 index.html
            app.MapFallbackToFile("/index.html");

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "SyZero.Example3.Web API V1");
                c.RoutePrefix = "api/swagger";
            });

            app.InitTables();
            app.Run();
        }
    }
}
