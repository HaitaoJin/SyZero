using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace SyZero.Authentication.ApiKey;

/// <summary>API Key 认证 scheme 配置。</summary>
public sealed class ApiKeyOptions : AuthenticationSchemeOptions
{
    public const string Scheme = "ApiKey";

    /// <summary>请求头名，默认 "X-Api-Key"。</summary>
    public string HeaderName { get; set; } = "X-Api-Key";

    /// <summary>合法 ApiKey → 主体名称（用于生成 ClaimsIdentity.Name）。</summary>
    public Dictionary<string, string> Keys { get; } = new(StringComparer.Ordinal);
}

internal sealed class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyOptions>
{
    public ApiKeyAuthenticationHandler(IOptionsMonitor<ApiKeyOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(Options.HeaderName, out var values) || string.IsNullOrWhiteSpace(values))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var key = values.ToString();
        if (!Options.Keys.TryGetValue(key, out var subject))
        {
            return Task.FromResult(AuthenticateResult.Fail("无效的 API Key"));
        }

        var claims = new[] { new Claim(ClaimTypes.Name, subject) };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
