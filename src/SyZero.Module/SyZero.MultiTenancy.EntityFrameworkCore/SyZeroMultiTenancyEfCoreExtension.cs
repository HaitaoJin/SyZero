using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace SyZero.MultiTenancy.EntityFrameworkCore;

public static class SyZeroMultiTenancyEfCoreExtension
{
    /// <summary>
    /// 注册基于 EF Core 的租户存储；调用方负责传入 <see cref="DbContextOptionsBuilder"/> 配置（连接字符串等）。
    /// </summary>
    public static IServiceCollection AddSyZeroMultiTenancyEfCore(this IServiceCollection services, Action<DbContextOptionsBuilder> optionsAction)
    {
        if (optionsAction == null) throw new ArgumentNullException(nameof(optionsAction));
        services.AddDbContext<TenantDbContext>(optionsAction);
        services.Replace(ServiceDescriptor.Scoped<ITenantStore, EfCoreTenantStore>());
        return services;
    }
}
