using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System;

namespace SyZero.Authorization.AspNetCore;

public static class SyZeroAuthorizationAspNetCoreExtension
{
    /// <summary>
    /// 注册 SyZero 权限到 ASP.NET Core 授权管线的桥接。
    /// 调用前请确保已注册 IPermissionChecker（SyZero.Core 默认提供 DefaultPermissionChecker）。
    /// </summary>
    public static IServiceCollection AddSyZeroAuthorizationBridge(this IServiceCollection services)
    {
        services.AddAuthorization();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IAuthorizationHandler, SyPermissionAuthorizationHandler>());
        return services;
    }

    /// <summary>注册标准 OIDC/OAuth 2.0 JWT Bearer 资源服务器，可对接 Keycloak、Entra ID、Auth0 等。</summary>
    public static IServiceCollection AddSyZeroOpenIdConnect(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = SyOpenIdConnectOptions.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var section = configuration.GetSection(sectionName);
        var options = section.Get<SyOpenIdConnectOptions>() ?? new SyOpenIdConnectOptions();
        if (!Uri.TryCreate(options.Authority, UriKind.Absolute, out _))
            throw new InvalidOperationException($"{sectionName}:Authority 必须是绝对 URI。");
        if (string.IsNullOrWhiteSpace(options.Audience))
            throw new InvalidOperationException($"{sectionName}:Audience 不能为空。");

        services.AddOptions<SyOpenIdConnectOptions>().Bind(section).ValidateOnStart();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(jwt =>
            {
                jwt.Authority = options.Authority;
                jwt.Audience = options.Audience;
                jwt.RequireHttpsMetadata = options.RequireHttpsMetadata;
                jwt.MapInboundClaims = false;
                jwt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    NameClaimType = options.NameClaimType,
                    RoleClaimType = options.RoleClaimType
                };
            });
        services.AddSyZeroAuthorizationBridge();
        return services;
    }
}
