using System.Threading.Tasks;
using SyZero.Mail;
using SyZero.Sms;
using Xunit;

namespace SyZero.Tests;

public class NotificationsTests
{
    [Fact]
    public async Task Console_mail_sender_does_not_throw()
    {
        ISyMailSender sender = new ConsoleMailSender();
        await sender.SendAsync("user@example.com", "Hi", "Hello world");
    }

    [Fact]
    public async Task Console_sms_sender_does_not_throw()
    {
        ISySmsSender sender = new ConsoleSmsSender();
        await sender.SendAsync("13800000000", "TEMPLATE_001", new { code = "1234" });
    }
}
