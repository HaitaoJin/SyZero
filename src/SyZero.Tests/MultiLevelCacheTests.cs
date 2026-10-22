using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using SyZero.Cache;
using SyZero.Serialization;
using Xunit;

namespace SyZero.Tests;

public class MultiLevelCacheTests
{
    private class FakeJson : IJsonSerialize
    {
        public T? JSONToObject<T>(string jsonText) => System.Text.Json.JsonSerializer.Deserialize<T>(jsonText);
        public string? ObjectToJSON(object obj) => System.Text.Json.JsonSerializer.Serialize(obj);
    }

    [Fact]
    public async Task GetOrCreate_FactoryCalledOnce_AndValueServedFromL1()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IJsonSerialize, FakeJson>();
        services.AddSyZeroMultiLevelCache(configure: o => { o.JitterRatio = 0; });
        var sp = services.BuildServiceProvider();
        var cache = sp.GetRequiredService<IMultiLevelCache>();

        var calls = 0;
        async Task<string?> Factory(CancellationToken ct)
        {
            await Task.Delay(5, ct);
            Interlocked.Increment(ref calls);
            return "value";
        }

        var v1 = await cache.GetOrCreateAsync<string>("k1", Factory, TimeSpan.FromMinutes(5));
        var v2 = await cache.GetOrCreateAsync<string>("k1", Factory, TimeSpan.FromMinutes(5));

        Assert.Equal("value", v1);
        Assert.Equal("value", v2);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task NullValue_CachedAsSentinel_WhenEnabled()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IJsonSerialize, FakeJson>();
        services.AddSyZeroMultiLevelCache(configure: o => { o.JitterRatio = 0; o.EnableNullCache = true; o.NullCacheSeconds = 5; });
        var sp = services.BuildServiceProvider();
        var cache = sp.GetRequiredService<IMultiLevelCache>();

        var calls = 0;
        Task<string?> Factory(CancellationToken ct) { Interlocked.Increment(ref calls); return Task.FromResult<string?>(null); }

        var v1 = await cache.GetOrCreateAsync<string>("nullk", Factory, TimeSpan.FromMinutes(5));
        var v2 = await cache.GetOrCreateAsync<string>("nullk", Factory, TimeSpan.FromMinutes(5));

        Assert.Null(v1);
        Assert.Null(v2);
        Assert.Equal(1, calls);
    }
}
