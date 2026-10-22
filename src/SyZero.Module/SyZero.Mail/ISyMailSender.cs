using System;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using SyZero.Dependency;

namespace SyZero.Mail;

/// <summary>
/// SyZero 邮件发送抽象。业务侧依赖此接口，避免直接耦合 SMTP / 第三方 SaaS。
/// </summary>
public interface ISyMailSender
{
    Task SendAsync(string to, string subject, string body, bool isHtml = false, CancellationToken cancellationToken = default);
}

/// <summary>SMTP 配置。</summary>
public sealed class SyMailOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 25;
    public bool EnableSsl { get; set; } = false;
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public string From { get; set; } = "noreply@example.com";
}

/// <summary>基于 <see cref="SmtpClient"/> 的默认实现。</summary>
public sealed class SmtpMailSender : ISyMailSender, ISingletonDependency
{
    private readonly SyMailOptions _options;

    public SmtpMailSender(SyMailOptions options) => _options = options ?? throw new ArgumentNullException(nameof(options));

    public async Task SendAsync(string to, string subject, string body, bool isHtml = false, CancellationToken cancellationToken = default)
    {
        using var client = new SmtpClient(_options.Host, _options.Port) { EnableSsl = _options.EnableSsl };
        if (!string.IsNullOrEmpty(_options.UserName))
        {
            client.Credentials = new NetworkCredential(_options.UserName, _options.Password);
        }
        using var message = new MailMessage(_options.From, to, subject, body) { IsBodyHtml = isHtml };
        await client.SendMailAsync(message, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>仅写日志的占位实现，便于开发环境与单测使用。</summary>
public sealed class ConsoleMailSender : ISyMailSender
{
    public Task SendAsync(string to, string subject, string body, bool isHtml = false, CancellationToken cancellationToken = default)
    {
        System.Console.WriteLine($"[Mail] -> {to}  Subject: {subject}  IsHtml: {isHtml}");
        System.Console.WriteLine(body);
        return Task.CompletedTask;
    }
}
