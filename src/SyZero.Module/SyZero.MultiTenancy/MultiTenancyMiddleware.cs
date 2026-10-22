using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace SyZero.MultiTenancy
{
    /// <summary>
    /// 多租户中间件：依次跑全部 <see cref="ITenantResolver"/>，命中后调 <see cref="ITenantStore"/> 取详情，
    /// 写入 <see cref="ICurrentTenant"/>，并保证请求结束时还原。
    /// </summary>
    public class MultiTenancyMiddleware
    {
        private readonly RequestDelegate _next;
        public MultiTenancyMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(
            HttpContext context,
            ICurrentTenant currentTenant,
            ITenantStore tenantStore,
            IEnumerable<ITenantResolver> resolvers)
        {
            TenantInfo? tenant = null;
            foreach (var r in resolvers)
            {
                var result = await r.ResolveAsync(context, context.RequestAborted).ConfigureAwait(false);
                if (result == null) continue;

                if (result.Id is { } id)
                    tenant = await tenantStore.FindByIdAsync(id, context.RequestAborted).ConfigureAwait(false);
                else if (!string.IsNullOrWhiteSpace(result.Name))
                    tenant = await tenantStore.FindByNameAsync(result.Name!, context.RequestAborted).ConfigureAwait(false);

                if (tenant is { IsActive: true }) break;
                tenant = null;
            }

            using (currentTenant.Change(tenant))
            {
                await _next(context).ConfigureAwait(false);
            }
        }
    }
}
