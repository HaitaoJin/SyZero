using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace SyZero.Swagger;

/// <summary>.NET 10 原生 OpenAPI 文档注册与端点映射。</summary>
public static class SyZeroOpenApiExtensions
{
    public static IServiceCollection AddSyZeroOpenApi(this IServiceCollection services, string documentName = "v1")
    {
        services.AddOpenApi(documentName);
        return services;
    }

    public static IEndpointConventionBuilder MapSyZeroOpenApi(
        this IEndpointRouteBuilder endpoints,
        string pattern = "/openapi/{documentName}.json")
        => endpoints.MapOpenApi(pattern);
}
