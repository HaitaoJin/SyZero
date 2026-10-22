# SyZero.AI

最小化 AI 抽象 `ISyChatClient`，本包内置 `EchoChatClient` 占位实现，便于本地开发 / 测试 / 离线场景；具体大模型适配（OpenAI、Azure OpenAI、Ollama 等）通过派生 `ISyChatClient` 注入即可。

## 📦 安装

```bash
dotnet add package SyZero.AI
```

## 🚀 快速开始

```csharp
builder.Services.AddSyZeroAIEcho();

public class ChatApp(ISyChatClient ai)
{
    public Task<string> AskAsync(string q, CancellationToken ct)
        => ai.CompleteAsync(q, ct);
}
```

`EchoChatClient` 行为：

```text
input  : "hello"
output : "[ai-echo] hello"
```

## 🧩 接入任意 `Microsoft.Extensions.AI.IChatClient`

本包内置 `MeaiChatClientAdapter`：把任意 `Microsoft.Extensions.AI.IChatClient` 实现（OpenAI / Azure OpenAI / Ollama / 自建网关 / 测试桩）一行适配为 `ISyChatClient`。业务代码继续依赖 `ISyChatClient`，便于 mock 与替换。

```csharp
// 直接传入实例
IChatClient openAi = new OpenAIChatClient(...);
builder.Services.AddSyZeroAIChatClient(openAi);

// 或通过工厂从 DI 解析依赖（HttpClient / Configuration 等）
builder.Services.AddSyZeroAIChatClient(sp =>
{
    var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient("openai");
    return new OpenAIChatClient(http, opt =>
    {
        opt.ApiKey = sp.GetRequiredService<IConfiguration>()["OpenAI:ApiKey"]!;
    });
});

// 业务代码不变
public class ChatApp(ISyChatClient ai)
{
    public Task<string> AskAsync(string q) => ai.CompleteAsync(q);
}
```

如果需要直接基于 `IChatClient` 写更复杂的逻辑（多轮上下文、function calling 等），可继续注入 `IChatClient` 本身；`AddSyZeroAIChatClient` 会同时把 `IChatClient` 与 `ISyChatClient` 都注册到 DI。

## 🧩 完全自定义 `ISyChatClient`

如果希望脱离 `Microsoft.Extensions.AI`，可直接派生 `ISyChatClient`：

```csharp
public sealed class CustomChatClient : ISyChatClient
{
    public Task<string> CompleteAsync(string prompt, CancellationToken ct)
        => /* call your own backend */;
}

services.AddSingleton<ISyChatClient, CustomChatClient>();
```

## 📦 依赖

仅声明 [`Microsoft.Extensions.AI`](https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai) 抽象包；具体 SDK（`Microsoft.Extensions.AI.OpenAI` / `OllamaSharp.Microsoft.Extensions.AI` 等）由用户按需添加。
