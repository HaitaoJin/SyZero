using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SyZero.DistributedTransaction;
using SyZero.DistributedTransaction.EntityFrameworkCore;
using System;

namespace SyZero;

/// <summary>SyZero.DistributedTransaction.EntityFrameworkCore 的 DI 扩展。</summary>
public static class SyZeroDistributedTransactionEfCoreExtension
{
    /// <summary>注册 OutboxDbContext 与基于 EF 的 IOutboxStore（替换默认 MemoryOutboxStore）。</summary>
    public static IServiceCollection AddSyZeroOutboxEfCore(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> optionsAction)
    {
        services.AddDbContext<OutboxDbContext>(optionsAction);
        services.Replace(ServiceDescriptor.Scoped<IOutboxStore, EfCoreOutboxStore>());
        services.Replace(ServiceDescriptor.Scoped<IInboxStore, EfCoreInboxStore>());
        return services;
    }
}
