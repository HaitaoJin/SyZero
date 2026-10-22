using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SyZero.Realtime;
using Xunit;

namespace SyZero.Tests;

public class RealtimeTests
{
    [Fact]
    public void AddSyZeroRealtime_registers_hub_context()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSyZeroRealtime();

        using var sp = services.BuildServiceProvider();
        var hub = sp.GetRequiredService<IHubContext<SyRealtimeHub>>();

        Assert.NotNull(hub);
    }
}
