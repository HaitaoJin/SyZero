using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace SyZero.MultiTenancy.Resolvers
{
    /// <summary>从请求头解析租户名（默认 Header：<c>__tenant</c>）。</summary>
    public class HeaderTenantResolver : ITenantResolver
    {
        private readonly MultiTenancyOptions _options;
        public HeaderTenantResolver(IOptions<MultiTenancyOptions> options) => _options = options.Value;

        public Task<TenantResolveResult?> ResolveAsync(HttpContext context, CancellationToken cancellationToken = default)
        {
            if (context.Request.Headers.TryGetValue(_options.TenantHeaderName, out var v))
            {
                var name = v.ToString();
                if (!string.IsNullOrWhiteSpace(name))
                    return Task.FromResult<TenantResolveResult?>(new TenantResolveResult { Name = name });
            }
            return Task.FromResult<TenantResolveResult?>(null);
        }
    }

    /// <summary>从子域名解析租户名（如 <c>tenant1.example.com</c> → <c>tenant1</c>）。</summary>
    public class DomainTenantResolver : ITenantResolver
    {
        private readonly MultiTenancyOptions _options;
        public DomainTenantResolver(IOptions<MultiTenancyOptions> options) => _options = options.Value;

        public Task<TenantResolveResult?> ResolveAsync(HttpContext context, CancellationToken cancellationToken = default)
        {
            var host = context.Request.Host.Host;
            if (string.IsNullOrEmpty(host)) return Task.FromResult<TenantResolveResult?>(null);
            if (!string.IsNullOrEmpty(_options.DomainSuffix) && host.EndsWith(_options.DomainSuffix!, System.StringComparison.OrdinalIgnoreCase))
            {
                var prefix = host[..^_options.DomainSuffix!.Length].TrimEnd('.');
                if (!string.IsNullOrWhiteSpace(prefix))
                    return Task.FromResult<TenantResolveResult?>(new TenantResolveResult { Name = prefix });
            }
            return Task.FromResult<TenantResolveResult?>(null);
        }
    }

    /// <summary>从已认证用户的 Claim 解析租户 Id / Name。</summary>
    public class ClaimTenantResolver : ITenantResolver
    {
        private readonly MultiTenancyOptions _options;
        public ClaimTenantResolver(IOptions<MultiTenancyOptions> options) => _options = options.Value;

        public Task<TenantResolveResult?> ResolveAsync(HttpContext context, CancellationToken cancellationToken = default)
        {
            var user = context.User;
            if (user?.Identity?.IsAuthenticated != true) return Task.FromResult<TenantResolveResult?>(null);

            var idClaim = user.FindFirst(_options.TenantIdClaimType);
            if (idClaim != null && long.TryParse(idClaim.Value, out var id))
                return Task.FromResult<TenantResolveResult?>(new TenantResolveResult { Id = id });

            var nameClaim = user.FindFirst(_options.TenantNameClaimType);
            if (nameClaim != null && !string.IsNullOrWhiteSpace(nameClaim.Value))
                return Task.FromResult<TenantResolveResult?>(new TenantResolveResult { Name = nameClaim.Value });

            return Task.FromResult<TenantResolveResult?>(null);
        }
    }
}
