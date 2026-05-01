using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SyZero.Gateway;
using SyZero.Gateway.Ocelot;
using System.Threading.Tasks;

namespace SyZero.Gateway
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.AddSyZero();
            builder.Configuration.AddJsonFile("configuration.json", optional: false, reloadOnChange: true);
            builder.WebHost.UseUrls($"{AppConfig.ServerOptions.Protocol}://*:{AppConfig.ServerOptions.Port}");

            builder.Logging.AddOpenTelemetry(logging =>
            {
                logging.IncludeFormattedMessage = true;
                logging.IncludeScopes = true;
            }).AddSyZeroLog4Net();

            builder.Services.AddSyZeroOpenTelemetry();
            builder.Services.AddSyZeroOcelot(options =>
            {
                options.SwaggerTitle = AppConfig.GetSection("SyZero:Name") ?? "SyZero.Gateway";
            }, builder.Configuration);
            builder.Services.AddSyZeroGatewayAdmin();

            var app = builder.Build();
            var gatewayOptions = app.Services.GetRequiredService<SyZeroOcelotOptions>();

            app.UseSyZero();
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseCors(gatewayOptions.CorsPolicyName);
            app.UseStaticFiles();

            // 网关管理面板的 Basic Auth 仅作用于 /admin、/admin-api，必须在 Ocelot 之前。
            app.UseSyZeroGatewayAdmin();

            app.MapControllers();
            app.MapRazorPages();

            // Ocelot 是终端中间件，会吞掉一切请求；用 MapWhen 让 /admin、/admin-api 走 MapControllers / MapRazorPages，
            // 其他请求才进入 Ocelot 网关分支。
            app.MapWhen(
                ctx => !ctx.Request.Path.StartsWithSegments("/admin")
                       && !ctx.Request.Path.StartsWithSegments("/admin-api"),
                branch => branch.UseSyZeroOcelotAsync().GetAwaiter().GetResult());

            await app.RunAsync();
        }
    }
}
