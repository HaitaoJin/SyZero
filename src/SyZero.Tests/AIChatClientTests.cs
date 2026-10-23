using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using SyZero.AI;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SyZero.Tests;

public class AIChatClientTests
{
    [Fact]
    public async Task EchoChatClient_ReturnsPrefixedPrompt()
    {
        var services = new ServiceCollection();
        services.AddSyZeroAIEcho();
        var sp = services.BuildServiceProvider();

        var client = sp.GetRequiredService<ISyChatClient>();
        var reply = await client.CompleteAsync("hello");
        Assert.Equal("[ai-echo] hello", reply);
    }

    [Fact]
    public async Task MeaiAdapter_BridgesAnyIChatClient()
    {
        var services = new ServiceCollection();
        services.AddSyZeroAIChatClient(new FakeMeaiChatClient());
        var sp = services.BuildServiceProvider();

        var client = sp.GetRequiredService<ISyChatClient>();
        var reply = await client.CompleteAsync("ping");
        Assert.Equal("[meai-fake] ping", reply);
    }

    private sealed class FakeMeaiChatClient : IChatClient
    {
        public ChatClientMetadata Metadata { get; } = new("fake");

        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            var lastUser = chatMessages.Last().Text ?? string.Empty;
            var msg = new ChatMessage(ChatRole.Assistant, $"[meai-fake] {lastUser}");
            return Task.FromResult(new ChatResponse(msg));
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            var lastUser = chatMessages.Last().Text ?? string.Empty;
            yield return new ChatResponseUpdate(ChatRole.Assistant, $"[meai-fake] {lastUser}");
        }

        public TService? GetService<TService>(object? key = null) where TService : class => null;
        public object? GetService(System.Type serviceType, object? serviceKey = null) => null;

        public void Dispose() { }
    }
}
