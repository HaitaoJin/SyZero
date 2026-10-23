using Microsoft.Extensions.DependencyInjection;
using SyZero.Mail;
using SyZero.Sms;

namespace SyZero.Notifications;

public sealed record NotificationMessage(
    string Recipient,
    string Subject,
    string Body,
    IReadOnlyCollection<string> Channels,
    IReadOnlyDictionary<string, object?>? Data = null);

public sealed record NotificationDeliveryResult(string Channel, bool Succeeded, string? Error = null);

public interface INotificationChannel
{
    string Name { get; }
    Task SendAsync(NotificationMessage message, CancellationToken cancellationToken = default);
}

public interface INotificationDispatcher
{
    Task<IReadOnlyList<NotificationDeliveryResult>> SendAsync(NotificationMessage message, CancellationToken cancellationToken = default);
}

public sealed class NotificationDispatcher(IEnumerable<INotificationChannel> channels) : INotificationDispatcher
{
    private readonly IReadOnlyDictionary<string, INotificationChannel> _channels =
        channels.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);

    public async Task<IReadOnlyList<NotificationDeliveryResult>> SendAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        var results = new List<NotificationDeliveryResult>();
        foreach (var name in message.Channels.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!_channels.TryGetValue(name, out var channel))
            {
                results.Add(new(name, false, "未注册通知通道。"));
                continue;
            }
            try
            {
                await channel.SendAsync(message, cancellationToken).ConfigureAwait(false);
                results.Add(new(name, true));
            }
            catch (Exception ex)
            {
                results.Add(new(name, false, ex.Message));
            }
        }
        return results;
    }
}

public sealed class MailNotificationChannel(ISyMailSender sender) : INotificationChannel
{
    public string Name => "mail";
    public Task SendAsync(NotificationMessage message, CancellationToken cancellationToken = default)
        => sender.SendAsync(message.Recipient, message.Subject, message.Body, true, cancellationToken);
}

public sealed class SmsNotificationChannel(ISySmsSender sender) : INotificationChannel
{
    public string Name => "sms";
    public Task SendAsync(NotificationMessage message, CancellationToken cancellationToken = default)
        => sender.SendAsync(message.Recipient, message.Subject, message.Data ?? new Dictionary<string, object?> { ["body"] = message.Body }, cancellationToken);
}

public static class SyZeroNotificationsExtension
{
    public static IServiceCollection AddSyZeroNotifications(this IServiceCollection services)
    {
        services.AddScoped<INotificationDispatcher, NotificationDispatcher>();
        services.AddScoped<INotificationChannel, MailNotificationChannel>();
        services.AddScoped<INotificationChannel, SmsNotificationChannel>();
        return services;
    }
}
