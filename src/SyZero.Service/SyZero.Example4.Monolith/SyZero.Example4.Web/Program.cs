using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SyZero;
using SyZero.DynamicWebApi;

var builder = WebApplication.CreateBuilder(args);

builder.AddSyZero();

builder.WebHost.UseUrls($"{AppConfig.ServerOptions.Protocol}://*:{AppConfig.ServerOptions.Port}");

builder.Services.AddControllers();
builder.Services.AddDynamicWebApi(new DynamicWebApiOptions
{
    DefaultApiPrefix = "/api",
    DefaultAreaName = AppConfig.ServerOptions.Name ?? string.Empty
});
builder.Services.AddSwagger();

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
    Service = AppConfig.ServerOptions.Name,
    AppConfig.ServerOptions.Port,
    Swagger = "/api/swagger",
    TodoApi = "/api/todo/list"
});

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "SyZero.Example4.Web API V1");
    options.RoutePrefix = "api/swagger";
});

app.Run();
