namespace SyZero.Authorization.AspNetCore;

/// <summary>外部 OpenID Connect/OAuth 2.0 身份提供方的资源服务器配置。</summary>
public sealed class SyOpenIdConnectOptions
{
    public const string SectionName = "SyZero:Authentication:OpenIdConnect";
    public string Authority { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public bool RequireHttpsMetadata { get; set; } = true;
    public string NameClaimType { get; set; } = "name";
    public string RoleClaimType { get; set; } = "role";
}
