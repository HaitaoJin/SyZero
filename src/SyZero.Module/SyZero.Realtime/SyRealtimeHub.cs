using Microsoft.AspNetCore.SignalR;
using SyZero.Dependency;
using System.Threading.Tasks;

namespace SyZero.Realtime;

/// <summary>
/// SyZero 默认实时通知 Hub。客户端通过订阅事件名（method）即可收到 <see cref="ISyRealtimeNotifier"/> 推送的消息。
/// </summary>
public class SyRealtimeHub : Hub
{
}

/// <summary>
/// 实时消息推送抽象，业务侧依赖此接口而非 IHubContext，以便单测/替换实现。
/// </summary>
public interface ISyRealtimeNotifier
{
    /// <summary>
    /// 向所有连接的客户端广播一条消息。
    /// </summary>
    Task BroadcastAsync(string method, object payload);

    /// <summary>
    /// 向指定 user（IUserIdProvider 解析后的标识）的所有连接推送消息。
    /// </summary>
    Task SendToUserAsync(string userId, string method, object payload);
}

internal sealed class SignalRRealtimeNotifier : ISyRealtimeNotifier, IScopedDependency
{
    private readonly IHubContext<SyRealtimeHub> _hub;
    public SignalRRealtimeNotifier(IHubContext<SyRealtimeHub> hub) => _hub = hub;

    public Task BroadcastAsync(string method, object payload)
        => _hub.Clients.All.SendAsync(method, payload);

    public Task SendToUserAsync(string userId, string method, object payload)
        => _hub.Clients.User(userId).SendAsync(method, payload);
}
