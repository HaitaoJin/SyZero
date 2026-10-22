using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using SyZero.Configurations;
using Xunit;

namespace SyZero.Tests;

public class ConfigurationChangedNotifierTests
{
    [Fact]
    public void Notify_TriggersChangedEvent()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var notifier = new ConfigurationChangedNotifier(config);
        var fired = 0;
        notifier.Changed += (_, _) => Interlocked.Increment(ref fired);

        notifier.Notify("Section:A");

        Assert.Equal(1, fired);
    }
}
