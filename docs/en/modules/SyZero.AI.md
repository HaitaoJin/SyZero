# SyZero.AI

Minimal AI abstraction `ISyChatClient` with a built-in `EchoChatClient` placeholder for local/test/offline scenarios. Provider integrations (OpenAI, Azure OpenAI, Ollama, etc.) plug in by deriving `ISyChatClient`.

## 📦 Install

```bash
dotnet add package SyZero.AI
```

## 🚀 Quick start

```csharp
builder.Services.AddSyZeroAIEcho();

public class ChatApp(ISyChatClient ai)
{
    public Task<string> AskAsync(string q, CancellationToken ct)
        => ai.CompleteAsync(q, ct);
}
```

`EchoChatClient` behaviour:

```text
input  : "hello"
output : "[ai-echo] hello"
```

## 🧩 Bridge any `Microsoft.Extensions.AI.IChatClient`

The package ships `MeaiChatClientAdapter` — adapt any `IChatClient` implementation (OpenAI / Azure OpenAI / Ollama / custom gateway / test stub) to `ISyChatClient` with one line. Business code keeps depending on `ISyChatClient` for easy mocking and replacement.

```csharp
// Pass an instance directly
IChatClient openAi = new OpenAIChatClient(...);
builder.Services.AddSyZeroAIChatClient(openAi);

// Or via a factory that resolves DI dependencies (HttpClient / Configuration / ...)
builder.Services.AddSyZeroAIChatClient(sp =>
{
    var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient("openai");
    return new OpenAIChatClient(http, opt =>
    {
        opt.ApiKey = sp.GetRequiredService<IConfiguration>()["OpenAI:ApiKey"]!;
    });
});

// Business code is unchanged
public class ChatApp(ISyChatClient ai)
{
    public Task<string> AskAsync(string q) => ai.CompleteAsync(q);
}
```

`AddSyZeroAIChatClient` registers both `IChatClient` and `ISyChatClient` so you can inject `IChatClient` directly when you need richer features (multi-turn context, function calling, etc.).

## 🧩 Fully custom implementation

If you want to bypass `Microsoft.Extensions.AI`, derive `ISyChatClient` yourself:

```csharp
public sealed class CustomChatClient : ISyChatClient
{
    public Task<string> CompleteAsync(string prompt, CancellationToken ct)
        => /* call your own backend */;
}

services.AddSingleton<ISyChatClient, CustomChatClient>();
```

The package only references the [`Microsoft.Extensions.AI`](https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai) abstraction; concrete provider SDKs (`Microsoft.Extensions.AI.OpenAI`, `OllamaSharp.Microsoft.Extensions.AI`, ...) are added by the consumer.
