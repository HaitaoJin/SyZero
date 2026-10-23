using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using SyZero;
using SyZero.DynamicWebApi;
using SyZero.Swagger;
using SyZero.Example4.Application.Todos;

var builder = WebApplication.CreateBuilder(args);

builder.AddSyZero();
var serverOptions = builder.Configuration.GetRequiredSection("SyZero").Get<SyZero.Configurations.SyZeroServerOptions>()
    ?? throw new InvalidOperationException("缺少 SyZero 配置节。");

builder.Services.AddControllers();
builder.Services.AddDynamicWebApi(new DynamicWebApiOptions
{
    DefaultApiPrefix = "/api",
    DefaultAreaName = serverOptions.Name ?? string.Empty
}, typeof(TodoAppService).Assembly);
builder.Services.AddSyZeroOpenApi();

var app = builder.Build();

app.UseSyZero();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseRouting();
app.MapControllers();

app.MapGet("/", () => new
{
    Service = serverOptions.Name,
    serverOptions.Port,
    OpenApi = "/openapi/v1.json",
    TodoApi = "/api/todo/list"
});

app.MapSyZeroOpenApi();

app.Run();
