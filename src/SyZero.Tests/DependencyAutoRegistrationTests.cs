using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SyZero.Dependency;
using SyZero.Extension;
using SyZero.Serialization;
using Xunit;

namespace SyZero.Tests;

/// <summary>
/// 锁定依赖自动扫描 / 注册的关键行为，防止以下两类回归：
/// 1) <see cref="ReflectionHelper.GetAssemblies"/> 必须能加载真实的运行时程序集
///    （历史 bug：用 lib.Name 调 Assembly.Load 失败时静默吞异常，导致 SyZero.Core
///     程序集从未被扫描）。
/// 2) ServiceCollectionExtensions 的标记接口自动注册能力
///    必须支持非泛型标记接口（IScopedDependency / ISingletonDependency /
///    ITransientDependency）。
/// </summary>
public class DependencyAutoRegistrationTests
{
    [Fact]
    public void GetAssemblies_Includes_SyZeroCoreAssembly()
    {
        var assemblies = ReflectionHelper.GetAssemblies().ToList();

        Assert.Contains(assemblies, a => a == typeof(DefaultJsonSerialize).Assembly);
    }

    [Fact]
    public void GetTypesAssignableTo_Picks_NonGeneric_MarkerInterface()
    {
        var asm = typeof(DefaultJsonSerialize).Assembly;

        var types = asm.GetTypesAssignableTo(typeof(ISingletonDependency));

        Assert.Contains(types, t => t.AsType() == typeof(DefaultJsonSerialize));
    }

    [Fact]
    public void AddClassesAsImplementedInterface_Registers_IJsonSerialize_From_SingletonDependency()
    {
        var services = new ServiceCollection();

        services.AddClassesAsImplementedInterface(
            typeof(ISingletonDependency),
            ServiceLifetime.Singleton);

        using var provider = services.BuildServiceProvider();
        var resolved = provider.GetService<IJsonSerialize>();

        Assert.NotNull(resolved);
        Assert.IsType<DefaultJsonSerialize>(resolved);
    }

    [Fact]
    public void AddClassesAsImplementedInterface_Registers_ConcreteMarkerDependency()
    {
        var services = new ServiceCollection();

        services.AddClassesAsImplementedInterface(
            typeof(IScopedDependency),
            ServiceLifetime.Scoped);

        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(SyZeroServiceDependencies)
            && descriptor.ImplementationType == typeof(SyZeroServiceDependencies)
            && descriptor.Lifetime == ServiceLifetime.Scoped);
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IScopedDependency));
    }
}
