namespace TicketHub.Application.Events;

public record TicketCreatedIntegrationEvent(int TicketId, string Title, string UserEmail, DateTime CreatedAt);
