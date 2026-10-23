using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace SyZero.AI;

/// <summary>
/// 把任意 <see cref="Microsoft.Extensions.AI.IChatClient"/>（OpenAI / Azure OpenAI / Ollama / 自建网关 等）
/// 适配为 SyZero 的 <see cref="ISyChatClient"/>。业务代码继续依赖 <c>ISyChatClient</c>，便于替换与单测。
/// </summary>
public sealed class MeaiChatClientAdapter : ISyChatClient
{
    private readonly IChatClient _inner;

    public MeaiChatClientAdapter(IChatClient inner)
    {
        _inner = inner ?? throw new System.ArgumentNullException(nameof(inner));
    }

    public async Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var messages = new System.Collections.Generic.List<ChatMessage>
        {
            new(ChatRole.User, prompt)
        };
        var response = await _inner.GetResponseAsync(messages, options: null, cancellationToken).ConfigureAwait(false);
        return response.Text ?? string.Empty;
    }

    public async IAsyncEnumerable<string> StreamAsync(string prompt, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var messages = new List<ChatMessage> { new(ChatRole.User, prompt) };
        await foreach (var update in _inner.GetStreamingResponseAsync(messages, options: null, cancellationToken).ConfigureAwait(false))
        {
            if (!string.IsNullOrEmpty(update.Text)) yield return update.Text;
        }
    }
}
