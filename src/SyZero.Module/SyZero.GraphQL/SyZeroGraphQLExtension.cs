using HotChocolate.Execution.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace SyZero;

/// <summary>SyZero.GraphQL 的 DI / 端点扩展（HotChocolate 包装）。</summary>
public static class SyZeroGraphQLExtension
{
    /// <summary>
    /// 注册 GraphQL 服务器；通过 <paramref name="configure"/> 追加 Query/Mutation/Subscription 类型。
    /// 默认占位 Query <c>SyZeroPingQuery</c> 暴露 <c>ping: String</c>。
    /// </summary>
    public static IRequestExecutorBuilder AddSyZeroGraphQL(this IServiceCollection services, Action<IRequestExecutorBuilder>? configure = null)
    {
        var builder = services.AddGraphQLServer().AddQueryType<SyZeroPingQuery>();
        configure?.Invoke(builder);
        return builder;
    }

    /// <summary>映射 GraphQL 端点（默认 /graphql）。</summary>
    public static IEndpointRouteBuilder MapSyZeroGraphQL(this IEndpointRouteBuilder endpoints, string pattern = "/graphql")
    {
        endpoints.MapGraphQL(pattern);
        return endpoints;
    }
}

/// <summary>占位 Query 类型，确保 schema 至少有一个根字段。</summary>
public sealed class SyZeroPingQuery
{
    public string Ping() => "pong";
}
