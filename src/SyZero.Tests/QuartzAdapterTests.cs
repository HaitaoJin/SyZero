using System.Text.Json;
using System.Threading.Tasks;
using SyZero.BackgroundJobs.Quartz;
using Xunit;

namespace SyZero.Tests;

public class QuartzAdapterTests
{
    [Fact]
    public void JobDataKeys_AreStable()
    {
        // 仅校验常量稳定性，避免后续误改导致老调度数据失效
        Assert.Equal("JobType", SyZeroQuartzJobAdapter.JobTypeKey);
        Assert.Equal("ArgsType", SyZeroQuartzJobAdapter.ArgsTypeKey);
        Assert.Equal("ArgsJson", SyZeroQuartzJobAdapter.ArgsJsonKey);
    }

    [Fact]
    public void ArgsRoundTrip_PreservesValue()
    {
        var args = new { Name = "hello", Count = 3 };
        var json = JsonSerializer.Serialize(args);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("hello", doc.RootElement.GetProperty("Name").GetString());
        Assert.Equal(3, doc.RootElement.GetProperty("Count").GetInt32());
    }
}
