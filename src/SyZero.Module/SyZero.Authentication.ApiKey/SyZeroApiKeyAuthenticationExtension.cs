using Microsoft.AspNetCore.Authentication;
using SyZero.Authentication.ApiKey;
using System;

namespace SyZero;

/// <summary>SyZero.Authentication.ApiKey 的扩展。</summary>
public static class SyZeroApiKeyAuthenticationExtension
{
    public static AuthenticationBuilder AddSyZeroApiKey(
        this AuthenticationBuilder builder,
        Action<ApiKeyOptions> configure,
        string scheme = ApiKeyOptions.Scheme)
    {
        return builder.AddScheme<ApiKeyOptions, ApiKeyAuthenticationHandler>(scheme, configure);
    }
}
