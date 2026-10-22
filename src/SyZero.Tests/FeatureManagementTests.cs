using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SyZero.FeatureManagement;
using Xunit;

namespace SyZero.Tests;

public class FeatureManagementTests
{
    [Fact]
    public async Task FeatureToggle_FromConfiguration_Works()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new System.Collections.Generic.Dictionary<string, string?>
            {
                ["FeatureManagement:NewCheckout"] = "true",
                ["FeatureManagement:OldCheckout"] = "false",
            })
            .Build();

        var sp = new ServiceCollection()
            .AddSyZeroFeatureManagement(config)
            .BuildServiceProvider();

        var fm = sp.GetRequiredService<ISyFeatureManager>();
        Assert.True(await fm.IsEnabledAsync("NewCheckout"));
        Assert.False(await fm.IsEnabledAsync("OldCheckout"));
    }
}
