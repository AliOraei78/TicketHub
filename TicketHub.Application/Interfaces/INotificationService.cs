using System.Collections.Generic;
using System.Threading.Tasks;
using TicketHub.Application.DTOs;

namespace TicketHub.Application.Interfaces;

public interface INotificationService
{
    Task<int> GetUnreadCountAsync(int userId);
    Task<List<NotificationDto>> GetUserNotificationsAsync(int userId, int page = 1, int pageSize = 20, bool? unreadOnly = null);
    Task<NotificationSummaryDto> GetSummaryAsync(int userId, int limit = 10);
    Task<NotificationDto?> GetByIdAsync(int id, int userId);
    Task<NotificationDto> CreateNotificationAsync(CreateNotificationDto dto);
    Task<List<NotificationDto>> CreateBulkNotificationAsync(IEnumerable<CreateNotificationDto> dtos);
    Task<bool> MarkAsReadAsync(int id, int userId);
    Task<int> MarkAllAsReadAsync(int userId);
    Task DeleteNotificationAsync(int id, int userId);
}
