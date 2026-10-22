using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SyZero.SourceGenerator;
using System.Linq;
using Xunit;

namespace SyZero.Tests;

public class SourceGeneratorTests
{
    [Fact]
    public void Generator_ProducesDiscoveredServicesFile()
    {
        const string source = @"
namespace Demo
{
    public interface IApplicationService { }
    public class FooService : IApplicationService { }
    public class BarService : IApplicationService { }
}";
        var compilation = CSharpCompilation.Create("test",
            new[] { CSharpSyntaxTree.ParseText(source) },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });

        var driver = CSharpGeneratorDriver.Create(new SyServiceMarkerGenerator());
        var result = driver.RunGenerators(compilation).GetRunResult();

        var generated = result.GeneratedTrees.FirstOrDefault(t => t.FilePath.EndsWith("SyDiscoveredServices.g.cs"));
        Assert.NotNull(generated);
        var text = generated!.ToString();
        Assert.Contains("FooService", text);
        Assert.Contains("BarService", text);
    }

    [Fact]
    public void DIRegistrationGenerator_EmitsLifetimeAwareRegistrations()
    {
        const string source = @"
namespace SyZero.Dependency
{
    public interface IScopedDependency { }
    public interface ISingletonDependency { }
    public interface ITransientDependency { }
}
namespace Demo
{
    using SyZero.Dependency;
    public interface IFooService { }
    public class FooService : IFooService, IScopedDependency { }
    public class CacheService : ISingletonDependency { }
    public class HelperService : ITransientDependency { }
    public abstract class IgnoredAbstract : IScopedDependency { }
}";
        var compilation = CSharpCompilation.Create("test",
            new[] { CSharpSyntaxTree.ParseText(source) },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });

        var driver = CSharpGeneratorDriver.Create(new SyDIRegistrationGenerator());
        var result = driver.RunGenerators(compilation).GetRunResult();

        var generated = result.GeneratedTrees.FirstOrDefault(t => t.FilePath.EndsWith("SyZeroGeneratedRegistration.g.cs"));
        Assert.NotNull(generated);
        var text = generated!.ToString();

        Assert.Contains("AddScoped<global::Demo.FooService>()", text);
        Assert.Contains("AddScoped<global::Demo.IFooService>(sp => sp.GetRequiredService<global::Demo.FooService>())", text);
        Assert.Contains("AddSingleton<global::Demo.CacheService>()", text);
        Assert.Contains("AddTransient<global::Demo.HelperService>()", text);
        Assert.DoesNotContain("IgnoredAbstract", text);
    }
}
