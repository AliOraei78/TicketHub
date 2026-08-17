using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TicketHub.Core.Interfaces;

namespace TicketHub.Infrastructure.Services;

public class SmtpEmailService : IEmailService
{
    private readonly ILogger<SmtpEmailService> _logger;
    private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;

    public SmtpEmailService(ILogger<SmtpEmailService> logger, Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        _logger.LogInformation("شروع ارسال ایمیل به {ToEmail} با موضوع: {Subject}", toEmail, subject);

        var host = _configuration["EmailSettings:Host"] ?? "smtp.gmail.com";
        var port = int.TryParse(_configuration["EmailSettings:Port"], out var p) ? p : 587;
        var userName = _configuration["EmailSettings:UserName"] ?? _configuration["EmailSettings:FromEmail"];
        var password = _configuration["EmailSettings:Password"];
        var fromEmail = _configuration["EmailSettings:FromEmail"] ?? userName ?? "noreply@tickethub.io";
        var fromName = _configuration["EmailSettings:FromName"] ?? "TicketHub System";

        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning("[SMTP MOCK/DISABLED] تنظیمات احراز هویت ایمیل (EmailSettings:UserName/Password) پیکربندی نشده است. ایمیل ارسالی به {ToEmail}: {Subject}", toEmail, subject);
            return;
        }

        try
        {
            using var smtpClient = new SmtpClient(host)
            {
                Port = port,
                Credentials = new NetworkCredential(userName, password),
                EnableSsl = true,
                Timeout = 5000 // 5 seconds timeout to prevent hanging
            };

            using var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
            };

            mailMessage.To.Add(toEmail);

            await smtpClient.SendMailAsync(mailMessage);

            _logger.LogInformation("ایمیل با موفقیت به {ToEmail} ارسال شد.", toEmail);
        }
        catch (Exception ex) when (ex is SmtpException or SocketException)
        {
            _logger.LogWarning(ex, "[SMTP FALLBACK] ارسال ایمیل به {ToEmail} به دلیل عدم دسترسی به سرور SMTP یا قطعی شبکه ناموفق بود. محتوای ایمیل جهت بررسی: {Body}", toEmail, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطای پیش‌بینی‌نشده در ارسال ایمیل به {ToEmail}", toEmail);
            throw;
        }
    }
}