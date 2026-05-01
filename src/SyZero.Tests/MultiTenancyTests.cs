using System.Threading.Tasks;
using SyZero.MultiTenancy;
using Xunit;

namespace SyZero.Tests;

public class MultiTenancyTests
{
    [Fact]
    public void CurrentTenant_Change_RestoresOnDispose()
    {
        var current = new CurrentTenant();
        Assert.Null(current.Id);

        using (current.Change(new TenantInfo { Id = 1, Name = "acme" }))
        {
            Assert.Equal(1, current.Id);
            Assert.Equal("acme", current.Name);
            Assert.True(current.IsAvailable);

            using (current.Change(new TenantInfo { Id = 2, Name = "globex" }))
            {
                Assert.Equal(2, current.Id);
            }

            // 内层 dispose 后应回到外层
            Assert.Equal(1, current.Id);
        }

        // 外层 dispose 后回到宿主侧
        Assert.Null(current.Id);
        Assert.False(current.IsAvailable);
    }

    [Fact]
    public async Task InMemoryTenantStore_AddAndFind_Works()
    {
        var store = new InMemoryTenantStore();
        store.AddOrUpdate(new TenantInfo { Id = 10, Name = "alpha", DisplayName = "Alpha Inc" });

        var byId = await store.FindByIdAsync(10);
        var byName = await store.FindByNameAsync("ALPHA");
        var miss = await store.FindByIdAsync(999);

        Assert.NotNull(byId);
        Assert.Equal("alpha", byId!.Name);
        Assert.NotNull(byName);
        Assert.Equal(10, byName!.Id);
        Assert.Null(miss);
    }
}
