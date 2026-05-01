using Mapster;
using Microsoft.Extensions.DependencyInjection;
using SyZero.Mapster;
using SyZero.ObjectMapper;
using Xunit;

namespace SyZero.Tests;

public class MapsterTests
{
    [Fact]
    public void AddSyZeroMapster_RegistersMapperAndMapsUsingProvidedAssembly()
    {
        var services = new ServiceCollection();
        services.AddSyZeroMapster(typeof(TestRegister).Assembly);

        using var provider = services.BuildServiceProvider();
        var objectMapper = provider.GetRequiredService<IObjectMapper>();

        var destination = objectMapper.Map<Destination>(new Source
        {
            Id = 7,
            Name = "alice",
            Values = new List<string> { "a", "b" }
        });

        Assert.IsType<MapsterObjectMapper>(objectMapper);
        Assert.Equal(7, destination.Id);
        Assert.Equal("alice", destination.Name);
        Assert.Equal(new[] { "a", "b" }, destination.Values);
    }

    [Fact]
    public void AddSyZeroMapster_WithConfigAction_AppliesCustomRule()
    {
        var services = new ServiceCollection();
        services.AddSyZeroMapster(config =>
        {
            config.NewConfig<Source, Destination>()
                .Map(dest => dest.Name, src => "mapped:" + src.Name);
        }, typeof(MapsterTests).Assembly);

        using var provider = services.BuildServiceProvider();
        var objectMapper = provider.GetRequiredService<IObjectMapper>();

        var destination = objectMapper.Map<Destination>(new Source
        {
            Id = 9,
            Name = "bob"
        });

        Assert.Equal(9, destination.Id);
        Assert.Equal("mapped:bob", destination.Name);
    }

    [Fact]
    public void AddSyZeroMapster_ReplacesExistingObjectMapperRegistration()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IObjectMapper, DefaultObjectMapper>();

        services.AddSyZeroMapster(typeof(TestRegister).Assembly);

        var registrations = services.Where(descriptor => descriptor.ServiceType == typeof(IObjectMapper)).ToList();

        Assert.Single(registrations);
        Assert.Equal(typeof(MapsterObjectMapper), registrations[0].ImplementationType);

        using var provider = services.BuildServiceProvider();
        Assert.IsType<MapsterObjectMapper>(provider.GetRequiredService<IObjectMapper>());
    }

    /// <summary>
    /// 仅供 <see cref="MapsterTests"/> 使用的 IRegister，验证程序集扫描可以拾取自定义映射规则。
    /// </summary>
    private sealed class TestRegister : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<Source, Destination>();
        }
    }

    private sealed class Source
    {
        public int Id { get; set; }

        public string? Name { get; set; }

        public List<string>? Values { get; set; }
    }

    private sealed class Destination
    {
        public int Id { get; set; }

        public string? Name { get; set; }

        public List<string>? Values { get; set; }
    }
}
