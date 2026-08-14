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

    public SmtpEmailService(ILogger<SmtpEmailService> logger)
    {
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        _logger.LogInformation("شروع ارسال ایمیل به {ToEmail} با موضوع: {Subject}", toEmail, subject);

        try
        {
            // Configure SMTP client settings (e.g., for Gmail)
            using var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential("jenabicoder@gmail.com", "mdipkbeemzzylldh"),
                EnableSsl = true,
                Timeout = 5000 // 5 seconds timeout to prevent hanging
            };

            // Prepare the email message
            using var mailMessage = new MailMessage
            {
                From = new MailAddress("jenabicoder@gmail.com", "TicketHub System"),
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
            };

            mailMessage.To.Add(toEmail);

            // Send the email asynchronously
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