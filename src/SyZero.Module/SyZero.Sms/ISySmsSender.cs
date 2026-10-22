using System.Threading;
using System.Threading.Tasks;

namespace SyZero.Sms;

/// <summary>SyZero 短信发送抽象。业务侧依赖此接口，避免直接耦合具体短信通道。</summary>
public interface ISySmsSender
{
    Task SendAsync(string phoneNumber, string templateCode, object templateParams, CancellationToken cancellationToken = default);
}

/// <summary>仅写日志的占位实现，便于开发环境与单测使用。</summary>
public sealed class ConsoleSmsSender : ISySmsSender
{
    public Task SendAsync(string phoneNumber, string templateCode, object templateParams, CancellationToken cancellationToken = default)
    {
        System.Console.WriteLine($"[SMS] -> {phoneNumber}  Template: {templateCode}  Params: {System.Text.Json.JsonSerializer.Serialize(templateParams)}");
        return Task.CompletedTask;
    }
}
