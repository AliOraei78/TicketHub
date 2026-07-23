using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using TicketHub.Core.Interfaces;

namespace TicketHub.Infrastructure.Services
{
    public class SmtpEmailService : IEmailService
    {
        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            // Configure SMTP client settings (e.g., for Gmail)
            var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential("jenabicoder@gmail.com", "mdipkbeemzzylldh"),
                EnableSsl = true,
            };

            // Prepare the email message
            var mailMessage = new MailMessage
            {
                From = new MailAddress("jenabicoder@gmail.com", "TicketHub System"),
                Subject = subject,
                Body = body,
                IsBodyHtml = true, // Set to true if your body contains HTML tags
            };

            mailMessage.To.Add(toEmail);

            // Send the email asynchronously
            await smtpClient.SendMailAsync(mailMessage);
        }
    }
}