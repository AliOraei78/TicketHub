using TicketHub.Application.DTOs;

namespace TicketHub.Web.Hubs;

public interface ITicketHubClient
{
    Task ReceiveNotification(NotificationDto notification);
    Task TicketUpdated(int ticketId);
    Task CommentAdded(int ticketId, int commentId);
}
