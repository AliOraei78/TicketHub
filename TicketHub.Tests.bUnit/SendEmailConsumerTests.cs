using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using TicketHub.Application.Events;
using TicketHub.Core.Interfaces;
using TicketHub.Infrastructure.Consumers;
using Xunit;

namespace TicketHub.Tests.bUnit;

public class SendEmailConsumerTests
{
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<ILogger<SendEmailConsumer>> _mockLogger;
    private readonly SendEmailConsumer _consumer;

    public SendEmailConsumerTests()
    {
        _mockEmailService = new Mock<IEmailService>();
        _mockLogger = new Mock<ILogger<SendEmailConsumer>>();
        _consumer = new SendEmailConsumer(_mockEmailService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Consume_SendsEmail_WhenUserEmailIsProvided()
    {
        // Arrange
        var @event = new TicketCreatedIntegrationEvent(101, "مشکل شبکه", "user@example.com", DateTime.UtcNow);
        var mockContext = new Mock<ConsumeContext<TicketCreatedIntegrationEvent>>();
        mockContext.Setup(c => c.Message).Returns(@event);

        // Act
        await _consumer.Consume(mockContext.Object);

        // Assert
        _mockEmailService.Verify(
            e => e.SendEmailAsync(
                "user@example.com",
                It.Is<string>(s => s.Contains("101") || s.Contains("مشکل شبکه")),
                It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task Consume_DoesNotSendEmail_WhenUserEmailIsEmpty()
    {
        // Arrange
        var @event = new TicketCreatedIntegrationEvent(102, "تیکت بدون ایمیل", "", DateTime.UtcNow);
        var mockContext = new Mock<ConsumeContext<TicketCreatedIntegrationEvent>>();
        mockContext.Setup(c => c.Message).Returns(@event);

        // Act
        await _consumer.Consume(mockContext.Object);

        // Assert
        _mockEmailService.Verify(
            e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }
}
