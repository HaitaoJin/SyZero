using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SyZero.Mail;
using System;

namespace SyZero;

/// <summary>SyZero.Mail 模块的 DI 扩展。</summary>
public static class SyZeroMailExtension
{
    public static IServiceCollection AddSyZeroSmtpMail(this IServiceCollection services, Action<SyMailOptions> configure)
    {
        var options = new SyMailOptions();
        configure(options);
        services.AddSingleton(options);
        services.TryAddSingleton<ISyMailSender, SmtpMailSender>();
        return services;
    }

    public static IServiceCollection AddSyZeroConsoleMail(this IServiceCollection services)
    {
        services.TryAddSingleton<ISyMailSender, ConsoleMailSender>();
        return services;
    }
}
