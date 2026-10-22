using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace SyZero.MultiTenancy.EntityFrameworkCore;

/// <summary>
/// EF Core 用的租户实体。业务可继承扩展自有字段。
/// </summary>
public class TenantEntity
{
    [Key]
    public long Id { get; set; }

    [Required]
    [MaxLength(64)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(128)]
    public string? DisplayName { get; set; }

    public bool IsActive { get; set; } = true;

    [MaxLength(1024)]
    public string? ConnectionString { get; set; }
}

/// <summary>
/// SyZero 多租户元数据 DbContext，业务可直接继承或合入主 DbContext。
/// </summary>
public class TenantDbContext : DbContext
{
    public TenantDbContext(DbContextOptions<TenantDbContext> options) : base(options) { }

    public DbSet<TenantEntity> Tenants => Set<TenantEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<TenantEntity>(b =>
        {
            b.ToTable("SyTenants");
            b.HasIndex(x => x.Name).IsUnique();
        });
    }
}
