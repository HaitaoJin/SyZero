using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SyZero.Gateway;
using SyZero.Gateway.Admin.Auditing;
using SyZero.Gateway.Admin.Auth;
using SyZero.Gateway.Admin.Events;
using SyZero.Gateway.Admin.Services;

namespace SyZero
{
    /// <summary>
    /// SyZero.Gateway.Admin 模块的注册入口。
    /// 提供网关管理面板（Razor Pages + REST API），路径前缀：/admin、/admin-api。
    /// 依赖 <see cref="IGateway"/> / <see cref="IGatewayConfigurationStore"/> 抽象，
    /// 由具体网关实现包（如 SyZero.Ocelot）提供后端。
    /// </summary>
    public static class SyZeroGatewayAdminExtension
    {
        /// <summary>
        /// 注册管理面板需要的服务（Razor Pages、Controllers）。多次调用安全。
        /// 注意：调用方需要确保已注册 <see cref="IGatewayConfigurationStore"/>（通常通过具体网关模块的 Add* 扩展）。
        /// 若未注册，则在缺省情况下兜底注册 <see cref="FileGatewayConfigurationStore"/>，但不会绑定 Applier。
        /// </summary>
        public static IServiceCollection AddSyZeroGatewayAdmin(this IServiceCollection services)
        {
            var assembly = typeof(SyZeroGatewayAdminExtension).Assembly;
            services.AddRazorPages().AddApplicationPart(assembly);
            services.AddControllers().AddApplicationPart(assembly);

            // 绑定限流配置（缺省即使用默认值）。手动读取避免引入 Configuration.Binder 依赖。
            services.AddOptions<GatewayAdminRateLimitOptions>()
                .Configure<Microsoft.Extensions.Configuration.IConfiguration>((opts, cfg) =>
                {
                    var section = cfg.GetSection(GatewayAdminRateLimitOptions.SectionName);
                    if (int.TryParse(section["MaxFailures"], out var mf)) opts.MaxFailures = mf;
                    if (int.TryParse(section["LockoutMinutes"], out var lm)) opts.LockoutMinutes = lm;
                    if (int.TryParse(section["WindowMinutes"], out var wm)) opts.WindowMinutes = wm;
                    if (int.TryParse(section["CaptchaThreshold"], out var ct)) opts.CaptchaThreshold = ct;
                    if (bool.TryParse(section["TrustForwardedFor"], out var tf)) opts.TrustForwardedFor = tf;
                    var headerName = section["ForwardedHeaderName"];
                    if (!string.IsNullOrWhiteSpace(headerName)) opts.ForwardedHeaderName = headerName!;
                });

            // 绑定 IP 黑白名单配置。手动读取数组（Configuration:Section:Allow:0/1/...）。
            services.AddOptions<GatewayAdminIpFilterOptions>()
                .Configure<Microsoft.Extensions.Configuration.IConfiguration>((opts, cfg) =>
                {
                    var section = cfg.GetSection(GatewayAdminIpFilterOptions.SectionName);
                    var allow = section.GetSection("Allow").GetChildren();
                    var deny = section.GetSection("Deny").GetChildren();
                    opts.Allow = new System.Collections.Generic.List<string>();
                    foreach (var c in allow) if (!string.IsNullOrWhiteSpace(c.Value)) opts.Allow.Add(c.Value!);
                    opts.Deny = new System.Collections.Generic.List<string>();
                    foreach (var c in deny) if (!string.IsNullOrWhiteSpace(c.Value)) opts.Deny.Add(c.Value!);
                    if (bool.TryParse(section["TrustForwardedFor"], out var tf)) opts.TrustForwardedFor = tf;
                    var hdr = section["ForwardedHeaderName"];
                    if (!string.IsNullOrWhiteSpace(hdr)) opts.ForwardedHeaderName = hdr!;
                });

            // 如果具体网关实现没有提供 Applier，则注册一个 no-op，避免 Admin 写入时抛 DI 异常。
            services.TryAddSingleton<IGatewayConfigurationApplier, NoOpGatewayConfigurationApplier>();
            // 如果具体网关实现没有提供 Store，则提供一个文件存储兜底。
            services.TryAddSingleton<IGatewayConfigurationStore, FileGatewayConfigurationStore>();
            // 审计日志：默认文件实现，可被替换。
            services.TryAddSingleton<IGatewayAdminAuditLog, FileGatewayAdminAuditLog>();
            // 登录失败限流：默认内存实现。
            services.TryAddSingleton<IFailedLoginTracker, FailedLoginTracker>();
            // 验证码存储：默认内存实现。
            services.TryAddSingleton<ICaptchaStore, MemoryCaptchaStore>();
            // 写操作事件总线（SSE 推送的源），单例 in-process。
            services.TryAddSingleton<IGatewayAdminEventBus, GatewayAdminEventBus>();

            // ---- 多用户与角色（兼容旧版单账号 Username/Password） ----
            services.AddOptions<GatewayAdminUsersOptions>()
                .Configure<Microsoft.Extensions.Configuration.IConfiguration>((opts, cfg) =>
                {
                    opts.Users = new System.Collections.Generic.List<GatewayAdminUser>();
                    var seen = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

                    // 1) 兼容旧版：Gateway:Admin:Username + Gateway:Admin:Password 视为内置 Admin 账号
                    var legacyName = cfg["Gateway:Admin:Username"];
                    var legacyPwd = cfg["Gateway:Admin:Password"];
                    if (!string.IsNullOrWhiteSpace(legacyName))
                    {
                        opts.Users.Add(new GatewayAdminUser
                        {
                            Name = legacyName!,
                            Password = legacyPwd ?? string.Empty,
                            Role = GatewayAdminRoles.Admin,
                        });
                        seen.Add(legacyName!);
                    }

                    // 2) Gateway:Admin:Users[] 数组（同名时数组项覆盖单账号项）
                    var section = cfg.GetSection(GatewayAdminUsersOptions.SectionName);
                    foreach (var child in section.GetChildren())
                    {
                        var name = child["Name"];
                        if (string.IsNullOrWhiteSpace(name)) continue;
                        // 数组同名覆盖单账号项
                        if (seen.Contains(name!))
                        {
                            opts.Users.RemoveAll(u => string.Equals(u.Name, name, System.StringComparison.OrdinalIgnoreCase));
                        }
                        seen.Add(name!);
                        opts.Users.Add(new GatewayAdminUser
                        {
                            Name = name!,
                            Password = child["Password"] ?? string.Empty,
                            Role = GatewayAdminRoles.Normalize(child["Role"]),
                        });
                    }
                });
            services.TryAddSingleton<IGatewayAdminUserStore, FileGatewayAdminUserStore>();
            services.TryAddSingleton<IUserResolver, UserResolver>();

            return services;
        }

        /// <summary>
        /// 切换登录失败追踪器到基于 <see cref="SyZero.Cache.ICache"/> 的实现，
        /// 让多实例网关共享失败计数与锁定状态。需在容器中已注册任意 <see cref="SyZero.Cache.ICache"/>
        /// （例如调用 <c>services.AddSyZeroRedis(...)</c>）后使用。
        /// </summary>
        /// <remarks>
        /// 必须在 <see cref="AddSyZeroGatewayAdmin"/> 之后调用——它会替换默认的内存 <see cref="FailedLoginTracker"/>。
        /// </remarks>
        public static IServiceCollection UseSyZeroGatewayAdminCachedRateLimit(this IServiceCollection services)
        {
            // 直接 Replace 掉默认的内存实现。
            for (int i = services.Count - 1; i >= 0; i--)
            {
                if (services[i].ServiceType == typeof(IFailedLoginTracker))
                {
                    services.RemoveAt(i);
                }
            }
            services.AddSingleton<IFailedLoginTracker, CachedFailedLoginTracker>();
            return services;
        }

        /// <summary>
        /// 在 /admin、/admin-api 路径前注入 Basic Auth 中间件。必须在 <c>UseRouting</c> 之后、
        /// 具体网关终端中间件（如 Ocelot）之前调用。
        /// </summary>
        public static IApplicationBuilder UseSyZeroGatewayAdmin(this IApplicationBuilder app)
        {
            // 启动时校验：必须至少配置 1 个账号，否则面板无法登录。
            var resolver = app.ApplicationServices.GetService(typeof(IUserResolver)) as IUserResolver;
            if (resolver != null)
            {
                var users = resolver.ListAsync(System.Threading.CancellationToken.None).GetAwaiter().GetResult();
                if (users.Count == 0)
                {
                    throw new System.InvalidOperationException(
                        "SyZero.Gateway.Admin 必须至少配置 1 个管理员账号。" +
                        System.Environment.NewLine +
                        "迁移示例（appsettings.json，二选一或并存）：" + System.Environment.NewLine +
                        "  // 单账号（兼容旧版）：" + System.Environment.NewLine +
                        "  \"Gateway\": { \"Admin\": { \"Username\": \"admin\", \"Password\": \"<密码>\" } }" + System.Environment.NewLine +
                        "  // 多账号 + 角色：" + System.Environment.NewLine +
                        "  \"Gateway\": { \"Admin\": { \"Users\": [ { \"Name\": \"admin\", \"Password\": \"<密码>\", \"Role\": \"Admin\" } ] } }");
                }
            }

            return app.UseWhen(
                ctx => ctx.Request.Path.StartsWithSegments("/admin")
                       || ctx.Request.Path.StartsWithSegments("/admin-api"),
                branch =>
                {
                    // IP 黑白名单先于 Auth 执行：未授权的来源直接 403，
                    // 也避免 Auth 端点（含 /admin-api/auth/captcha 匿名路径）泄露给被拒 IP。
                    branch.UseMiddleware<AdminIpFilterMiddleware>();
                    branch.UseMiddleware<AdminBasicAuthMiddleware>();
                });
        }
    }
}
