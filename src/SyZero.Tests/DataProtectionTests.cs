using Microsoft.Extensions.DependencyInjection;
using SyZero;
using SyZero.DataProtection;
using Xunit;

namespace SyZero.Tests;

public class DataProtectionTests
{
    [Fact]
    public void Should_RoundTrip_Plaintext()
    {
        var services = new ServiceCollection();
        services.AddSyZeroDataProtection();
        var sp = services.BuildServiceProvider();

        var enc = sp.GetRequiredService<IFieldEncryptor>();

        var cipher = enc.Protect("secret-token");
        Assert.NotNull(cipher);
        Assert.NotEqual("secret-token", cipher);

        var plain = enc.Unprotect(cipher);
        Assert.Equal("secret-token", plain);
    }

    [Fact]
    public void Different_Purposes_Should_Be_Isolated()
    {
        var services = new ServiceCollection();
        services.AddSyZeroDataProtection();
        var sp = services.BuildServiceProvider();

        var enc = sp.GetRequiredService<IFieldEncryptor>();
        var cipher = enc.Protect("hello", "purpose-A");

        Assert.ThrowsAny<System.Exception>(() => enc.Unprotect(cipher, "purpose-B"));
    }
}
