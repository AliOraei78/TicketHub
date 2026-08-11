using System.Collections.Generic;
using System.Threading.Tasks;
using TicketHub.Core.Entities;

namespace TicketHub.Core.Interfaces;

public interface INotificationRepository : IRepository<Notification>
{
    Task<int> GetUnreadCountAsync(int userId);
    Task<List<Notification>> GetUserNotificationsAsync(int userId, int page = 1, int pageSize = 20, bool? unreadOnly = null);
    Task<bool> MarkAsReadAsync(int id, int userId);
    Task<int> MarkAllAsReadAsync(int userId);
}
