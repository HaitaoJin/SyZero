using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace SyZero.MultiTenancy;

/// <summary>把当前租户自动传播到服务间 HttpClient 调用。</summary>
public sealed class TenantPropagationHandler(ICurrentTenant currentTenant, IOptions<MultiTenancyOptions> options) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (currentTenant.Id is { } id)
        {
            request.Headers.Remove(options.Value.TenantHeaderName);
            request.Headers.TryAddWithoutValidation(options.Value.TenantHeaderName, id.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
        return base.SendAsync(request, cancellationToken);
    }
}
