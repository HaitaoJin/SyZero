using System;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SyZero.Authorization.Entities;
using SyZero.Authorization.Jwt;
using SyZero.Authorization.Permissions;
using SyZero.Authorization.Stores;
using SyZero.Runtime.Security;

namespace SyZero.Authorization
{
    /// <summary>
    /// SyZero.Authorization 注册与启用扩展。
    /// </summary>
    public static class SyZeroAuthorizationExtension
    {
        /// <summary>
        /// 注册 SyZero RBAC + JWT + ASP.NET Core Identity Manager（基于 SyZero 仓储）。
        /// 使用前请确保已注册 IRepository&lt;User/Role/UserRole/RolePermission/UserPermission/Permission/PermissionGroup&gt; Provider。
        /// </summary>
        public static IServiceCollection AddSyZeroAuthorization(
            this IServiceCollection services,
            IConfiguration? configuration = null,
            Action<JwtOptions>? configureJwt = null)
        {
            // JwtOptions 配置
            if (configuration != null)
            {
                services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
            }
            if (configureJwt != null)
            {
                services.Configure(configureJwt);
            }

            // ASP.NET Core Identity（仅 Manager 部分，Stores 走 SyZero 仓储）
            services.AddScoped<IUserStore<User>, SyZeroUserStore>();
            services.AddScoped<IRoleStore<Role>, SyZeroRoleStore>();
            services.AddIdentityCore<User>(opt =>
            {
                opt.User.RequireUniqueEmail = false;
                opt.Password.RequireDigit = true;
                opt.Password.RequiredLength = 6;
                opt.Password.RequireLowercase = false;
                opt.Password.RequireUppercase = false;
                opt.Password.RequireNonAlphanumeric = false;
            })
            .AddRoles<Role>()
            .AddDefaultTokenProviders();

            // 替换 SyZero 默认 PermissionChecker 为 RBAC 版本
            services.AddScoped<IPermissionChecker, RbacPermissionChecker>();

            // PermissionDefinition 启动同步
            services.AddHostedService<PermissionSyncHostedService>();

            // JwtBearer 鉴权（HMAC-SHA256）
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    var sp = services.BuildServiceProvider();
                    var jwt = new JwtOptions();
                    configuration?.GetSection("Jwt").Bind(jwt);
                    configureJwt?.Invoke(jwt);
                    options.RequireHttpsMetadata = false;
                    options.SaveToken = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwt.Issuer,
                        ValidateAudience = true,
                        ValidAudience = jwt.Audience,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey)),
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromSeconds(30)
                    };
                });

            services.AddAuthorization();
            return services;
        }

        /// <summary>
        /// 启用 SyZero 鉴权管线（必须在 UseRouting 之后、UseEndpoints 之前调用）。
        /// </summary>
        public static IApplicationBuilder UseSyZeroAuthorization(this IApplicationBuilder app)
        {
            app.UseAuthentication();
            app.UseAuthorization();
            return app;
        }
    }
}
