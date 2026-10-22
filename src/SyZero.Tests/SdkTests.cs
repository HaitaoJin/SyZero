using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using SyZero.Sdk;
using Xunit;

namespace SyZero.Tests;

public class SdkTests
{
    private sealed class DemoClient : SyServiceClient
    {
        public DemoClient(HttpClient http) : base(http) { }
        public Uri? BaseAddress => Http.BaseAddress;
    }

    [Fact]
    public void AddSyServiceClient_sets_base_address()
    {
        var services = new ServiceCollection();
        services.AddSyServiceClient<DemoClient>("https://api.example.com/");

        using var sp = services.BuildServiceProvider();
        var client = sp.GetRequiredService<DemoClient>();

        Assert.Equal(new Uri("https://api.example.com/"), client.BaseAddress);
    }
}
