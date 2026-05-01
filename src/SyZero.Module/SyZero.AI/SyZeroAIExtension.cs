using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SyZero.AI;

namespace SyZero;

/// <summary>SyZero.AI 的 DI 扩展。</summary>
public static class SyZeroAIExtension
{
    /// <summary>注册占位 EchoChatClient（适用于本地、测试或离线场景）。</summary>
    public static IServiceCollection AddSyZeroAIEcho(this IServiceCollection services)
    {
        services.TryAddSingleton<ISyChatClient, EchoChatClient>();
        return services;
    }

    /// <summary>
    /// 通过实例注入一个 <see cref="IChatClient"/> 实现（来自 OpenAI / Azure OpenAI / Ollama 等 SDK），
    /// 自动用 <see cref="MeaiChatClientAdapter"/> 适配为 <see cref="ISyChatClient"/>。
    /// </summary>
    public static IServiceCollection AddSyZeroAIChatClient(this IServiceCollection services, IChatClient chatClient)
    {
        services.TryAddSingleton(chatClient);
        services.TryAddSingleton<ISyChatClient>(sp => new MeaiChatClientAdapter(sp.GetRequiredService<IChatClient>()));
        return services;
    }

    /// <summary>
    /// 通过工厂注入 <see cref="IChatClient"/>（便于在 DI 中读取配置 / IHttpClientFactory），
    /// 自动用 <see cref="MeaiChatClientAdapter"/> 适配为 <see cref="ISyChatClient"/>。
    /// </summary>
    public static IServiceCollection AddSyZeroAIChatClient(this IServiceCollection services, System.Func<System.IServiceProvider, IChatClient> factory)
    {
        services.TryAddSingleton(factory);
        services.TryAddSingleton<ISyChatClient>(sp => new MeaiChatClientAdapter(sp.GetRequiredService<IChatClient>()));
        return services;
    }
}
