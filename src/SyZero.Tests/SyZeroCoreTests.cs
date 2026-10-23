using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SyZero.Configurations;
using Moq;
using SyZero.Runtime.Security;
using SyZero.Runtime.Session;
using SyZero.Serialization;
using Xunit;

namespace SyZero.Tests;

public class SyZeroCoreTests
{
    [Fact]
    public void AddSyZero_BindsServerOptions_FromConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["SyZero:Name"] = "test-service" })
            .Build();
        var services = new ServiceCollection();
        services.AddSyZero(configuration);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<SyZeroServerOptions>>().Value;

        Assert.Equal("test-service", options.Name);
        Assert.Equal(ProtocolType.HTTP, options.Protocol);
    }

    [Fact]
    public void AddSyZero_RegistersCoreFallbacks_ForHostsWithoutOptionalModules()
    {
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddSyZero(configuration);

        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IToken));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(SyZero.Domain.Repository.IUnitOfWork));
    }

    [Fact]
    public void SySession_ParseClaimsPrincipal_HandlesMissingIdentity_AndReadsClaims()
    {
        var session = (ISySession)new SySession(new DefaultJsonSerialize(), Mock.Of<IToken>());

        session.Parse(new ClaimsPrincipal());

        Assert.Null(session.UserId);
        Assert.Null(session.UserRole);
        Assert.Null(session.UserName);
        Assert.Null(session.Permission);
        Assert.Null(session.Token);

        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(SyClaimTypes.UserId, "42"),
            new Claim(SyClaimTypes.UserRole, "admin"),
            new Claim(SyClaimTypes.UserName, "alice"),
            new Claim(SyClaimTypes.Permission, "[\"read\",\"write\"]"),
            new Claim(SyClaimTypes.Token, "jwt-token")
        }, "test"));

        session.Parse(principal);

        Assert.Equal(42, session.UserId);
        Assert.Equal("admin", session.UserRole);
        Assert.Equal("alice", session.UserName);
        Assert.Equal(new[] { "read", "write" }, session.Permission);
        Assert.Equal("jwt-token", session.Token);
    }

    [Fact]
    public void SySession_ParseToken_UsesRegisteredTokenService()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(SyClaimTypes.UserId, "7")
        }, "token"));

        var tokenMock = new Mock<IToken>();
        tokenMock.Setup(token => token.GetPrincipal("access-token")).Returns(principal);

        var session = new SySession(new DefaultJsonSerialize(), tokenMock.Object);

        session.Parse("access-token");

        Assert.Equal(7, session.UserId);
        tokenMock.Verify(token => token.GetPrincipal("access-token"), Times.Once);
    }

}
