using Microsoft.Extensions.DependencyInjection;
using SyZero;
using SyZero.Audit;
using Xunit;

namespace SyZero.Tests;

public class AuditTests
{
    private class LoginDto
    {
        public string UserName { get; set; } = string.Empty;

        [Sensitive]
        public string Password { get; set; } = string.Empty;
    }

    [Fact]
    public async Task MemoryAuditLogStore_Should_Save_And_Query()
    {
        var services = new ServiceCollection();
        services.AddSyZeroAudit();
        var sp = services.BuildServiceProvider();

        var store = sp.GetRequiredService<IAuditLogStore>();
        await store.SaveAsync(new AuditLogEntry { Action = "Test", Resource = "X" });

        var list = await store.QueryAsync(10);
        Assert.Single(list);
        Assert.Equal("Test", list[0].Action);
    }

    [Fact]
    public void DefaultMasker_Should_Mask_SensitiveProperty()
    {
        var masker = new DefaultAuditDataMasker();
        var json = masker.Serialize(new LoginDto { UserName = "alice", Password = "p@ssw0rd" });

        Assert.Contains("alice", json);
        Assert.Contains("***", json);
        Assert.DoesNotContain("p@ssw0rd", json);
    }
}
