using Microsoft.Extensions.DependencyInjection;
using SyZero.Localization;
using Xunit;

namespace SyZero.Tests;

public class LocalizationTests
{
    [Fact]
    public void MissingKey_FallsBackToKeyItself()
    {
        var sp = new ServiceCollection()
            .AddLogging()
            .AddSyZeroLocalization()
            .BuildServiceProvider();

        var loc = sp.GetRequiredService<ISyStringLocalizer>();
        // 没有 .resx 资源时，IStringLocalizer 默认返回 key 本身
        Assert.Equal("Hello", loc["Hello"]);
    }
}
