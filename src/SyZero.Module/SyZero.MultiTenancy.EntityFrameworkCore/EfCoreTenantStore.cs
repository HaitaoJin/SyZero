using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyZero.Dependency;

namespace SyZero.MultiTenancy.EntityFrameworkCore;

/// <summary>
/// 基于 EF Core 的 <see cref="ITenantStore"/> 持久化实现。
/// </summary>
public class EfCoreTenantStore : ITenantStore, IScopedDependency
{
    private readonly TenantDbContext _db;

    public EfCoreTenantStore(TenantDbContext db)
    {
        _db = db;
    }

    public async Task<TenantInfo?> FindByIdAsync(long id, CancellationToken ct = default)
    {
        var t = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return t == null ? null : Map(t);
    }

    public async Task<TenantInfo?> FindByNameAsync(string name, CancellationToken ct = default)
    {
        var t = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(x => x.Name == name, ct);
        return t == null ? null : Map(t);
    }

    private static TenantInfo Map(TenantEntity t) => new()
    {
        Id = t.Id,
        Name = t.Name,
        DisplayName = t.DisplayName,
        IsActive = t.IsActive,
        ConnectionString = t.ConnectionString,
    };
}
