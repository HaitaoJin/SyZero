using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyZero.MultiTenancy.EntityFrameworkCore;
using Xunit;

namespace SyZero.Tests;

public class MultiTenancyEfCoreTests
{
    private static TenantDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;
        var ctx = new TenantDbContext(options);
        ctx.Database.OpenConnection();
        ctx.Database.EnsureCreated();
        return ctx;
    }

    [Fact]
    public async Task FindByName_ReturnsMappedTenantInfo()
    {
        using var ctx = CreateContext();
        ctx.Tenants.Add(new TenantEntity { Name = "acme", DisplayName = "ACME", IsActive = true, ConnectionString = "DataSource=acme.db" });
        await ctx.SaveChangesAsync();

        var store = new EfCoreTenantStore(ctx);
        var t = await store.FindByNameAsync("acme");
        Assert.NotNull(t);
        Assert.Equal("ACME", t!.DisplayName);
        Assert.True(t.IsActive);
        Assert.Equal("DataSource=acme.db", t.ConnectionString);
    }

    [Fact]
    public async Task FindById_ReturnsNullWhenMissing()
    {
        using var ctx = CreateContext();
        var store = new EfCoreTenantStore(ctx);
        var t = await store.FindByIdAsync(9999);
        Assert.Null(t);
    }
}
