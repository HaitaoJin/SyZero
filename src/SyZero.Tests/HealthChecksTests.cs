using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SyZero;
using SyZero.HealthChecks;
using Xunit;

namespace SyZero.Tests;

public class HealthChecksTests
{
    [Fact]
    public void AddSyZeroHealthChecks_Should_Register_Options_And_HealthCheckService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();
        services.AddSyZeroHealthChecks(configure: o =>
        {
            o.LivePath = "/healthz/live";
            o.ReadyPath = "/healthz/ready";
        });

        var sp = services.BuildServiceProvider();

        var options = sp.GetRequiredService<SyZeroHealthChecksOptions>();
        Assert.Equal("/healthz/live", options.LivePath);
        Assert.Equal("/healthz/ready", options.ReadyPath);

        Assert.NotNull(sp.GetService<HealthCheckService>());
    }
}
