using System.Net;
using System.Net.Mail;
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

        // Configure SMTP client settings (e.g., for Gmail)
        using var smtpClient = new SmtpClient("smtp.gmail.com")
        {
            Port = 587,
            Credentials = new NetworkCredential("jenabicoder@gmail.com", "mdipkbeemzzylldh"),
            EnableSsl = true,
        };

        // Prepare the email message
        using var mailMessage = new MailMessage
        {
            From = new MailAddress("jenabicoder@gmail.com", "TicketHub System"),
            Subject = subject,
            Body = body,
            IsBodyHtml = true, // Set to true if your body contains HTML tags
        };

        mailMessage.To.Add(toEmail);

        // Send the email asynchronously
        await smtpClient.SendMailAsync(mailMessage);

        _logger.LogInformation("ایمیل با موفقیت به {ToEmail} ارسال شد.", toEmail);
    }
}