using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;
using SyZero;
using SyZero.Validation;
using Xunit;

namespace SyZero.Tests;

public class ValidationTests
{
    private class CreateUserDto
    {
        [Required]
        public string? UserName { get; set; }

        [Range(1, 200)]
        public int Age { get; set; }
    }

    [Fact]
    public async Task Runner_Should_Fallback_To_DataAnnotations_And_Detect_Errors()
    {
        var services = new ServiceCollection();
        services.AddSyZeroValidation();
        var sp = services.BuildServiceProvider();

        var runner = sp.GetRequiredService<ISyValidatorRunner>();
        var dto = new CreateUserDto { UserName = null, Age = 0 };

        var result = await runner.ValidateAsync(dto);

        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public async Task Runner_Should_Pass_When_Valid()
    {
        var services = new ServiceCollection();
        services.AddSyZeroValidation();
        var sp = services.BuildServiceProvider();

        var runner = sp.GetRequiredService<ISyValidatorRunner>();
        var dto = new CreateUserDto { UserName = "alice", Age = 18 };

        var result = await runner.ValidateAsync(dto);
        Assert.True(result.IsValid);
    }
}
