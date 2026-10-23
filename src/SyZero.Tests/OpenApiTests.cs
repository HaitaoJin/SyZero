using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using SyZero.Swagger;
using Xunit;

namespace SyZero.Tests;

public sealed class OpenApiTests
{
    [Fact]
    public void NativeOpenApi_RegistersDocumentEndpoint()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSyZeroOpenApi();
        var app = builder.Build();
        app.MapSyZeroOpenApi();

        var endpoints = ((IEndpointRouteBuilder)app).DataSources.SelectMany(x => x.Endpoints);
        Assert.Contains(endpoints, x => x.DisplayName?.Contains("OpenAPI", System.StringComparison.OrdinalIgnoreCase) == true);
    }
}
