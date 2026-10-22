using System.Threading;
using System.Threading.Tasks;

namespace SyZero.AI;

/// <summary>SyZero AI 抽象的最小聊天接口。</summary>
public interface ISyChatClient
{
    Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken = default);
}

/// <summary>占位实现：将 prompt 原样回显，用于测试与本地开发。</summary>
public sealed class EchoChatClient : ISyChatClient
{
    public Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken = default)
        => Task.FromResult($"[ai-echo] {prompt}");
}
