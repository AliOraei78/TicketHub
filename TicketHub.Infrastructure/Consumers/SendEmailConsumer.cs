using MassTransit;
using Microsoft.Extensions.Logging;
using TicketHub.Application.Events;
using TicketHub.Core.Interfaces;

namespace TicketHub.Infrastructure.Consumers;

public class SendEmailConsumer : IConsumer<TicketCreatedIntegrationEvent>
{
    private readonly IEmailService _emailService;
    private readonly ILogger<SendEmailConsumer> _logger;

    public SendEmailConsumer(IEmailService emailService, ILogger<SendEmailConsumer> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TicketCreatedIntegrationEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("Processing idempotent TicketCreatedIntegrationEvent for Ticket #{TicketId}", message.TicketId);

        if (!string.IsNullOrEmpty(message.UserEmail))
        {
            var subject = $"تیکت جدید ثبت شد: {message.Title}";
            var body = $"سلام، تیکت شما با شماره #{message.TicketId} با موفقیت ثبت شد.";
            await _emailService.SendEmailAsync(message.UserEmail, subject, body);
            _logger.LogInformation("Email successfully sent to {Email} for Ticket #{TicketId}", message.UserEmail, message.TicketId);
        }
    }
}
