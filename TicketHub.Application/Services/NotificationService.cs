using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mapster;
using Microsoft.Extensions.Logging;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Core.Common.Exceptions;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;

namespace TicketHub.Application.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ITicketEventBroker _eventBroker;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        INotificationRepository notificationRepository,
        ITicketEventBroker eventBroker,
        ILogger<NotificationService> logger)
    {
        _notificationRepository = notificationRepository;
        _eventBroker = eventBroker;
        _logger = logger;
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        if (userId <= 0) return 0;
        return await _notificationRepository.GetUnreadCountAsync(userId);
    }

    public async Task<List<NotificationDto>> GetUserNotificationsAsync(int userId, int page = 1, int pageSize = 20, bool? unreadOnly = null)
    {
        if (userId <= 0) return new List<NotificationDto>();

        var notifications = await _notificationRepository.GetUserNotificationsAsync(userId, page, pageSize, unreadOnly);
        return notifications.Adapt<List<NotificationDto>>();
    }

    public async Task<NotificationSummaryDto> GetSummaryAsync(int userId, int limit = 10)
    {
        if (userId <= 0) return new NotificationSummaryDto();

        var unreadCount = await _notificationRepository.GetUnreadCountAsync(userId);
        var recent = await _notificationRepository.GetUserNotificationsAsync(userId, 1, limit, null);

        return new NotificationSummaryDto
        {
            UnreadCount = unreadCount,
            RecentNotifications = recent.Adapt<List<NotificationDto>>()
        };
    }

    public async Task<NotificationDto?> GetByIdAsync(int id, int userId)
    {
        var notification = await _notificationRepository.GetByIdAsync(id);
        if (notification == null || notification.UserId != userId)
        {
            return null;
        }

        return notification.Adapt<NotificationDto>();
    }

    public async Task<NotificationDto> CreateNotificationAsync(CreateNotificationDto dto)
    {
        if (dto.UserId <= 0)
        {
            throw new ValidationException("شناسه کاربر گیرنده اعلان نامعتبر است.");
        }

        var notification = new Notification
        {
            UserId = dto.UserId,
            Title = dto.Title.Trim(),
            Message = dto.Message.Trim(),
            Type = dto.Type,
            Severity = dto.Severity,
            ReferenceId = dto.ReferenceId,
            ActionUrl = dto.ActionUrl,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        await _notificationRepository.AddAsync(notification);
        await _notificationRepository.SaveChangesAsync();

        _logger.LogInformation("اعلان جدید {NotificationId} برای کاربر {UserId} ایجاد شد.", notification.Id, dto.UserId);

        var resultDto = notification.Adapt<NotificationDto>();

        // انتشار بلادرنگ به کاربر هدف
        await _eventBroker.PublishNotificationAsync(dto.UserId, resultDto);

        return resultDto;
    }

    public async Task<List<NotificationDto>> CreateBulkNotificationAsync(IEnumerable<CreateNotificationDto> dtos)
    {
        var resultList = new List<NotificationDto>();
        foreach (var dto in dtos)
        {
            if (dto.UserId <= 0) continue;
            var created = await CreateNotificationAsync(dto);
            resultList.Add(created);
        }
        return resultList;
    }

    public async Task<bool> MarkAsReadAsync(int id, int userId)
    {
        if (id <= 0 || userId <= 0) return false;

        var success = await _notificationRepository.MarkAsReadAsync(id, userId);
        if (success)
        {
            _logger.LogInformation("اعلان {NotificationId} توسط کاربر {UserId} خوانده شد.", id, userId);
        }
        return success;
    }

    public async Task<int> MarkAllAsReadAsync(int userId)
    {
        if (userId <= 0) return 0;

        var count = await _notificationRepository.MarkAllAsReadAsync(userId);
        _logger.LogInformation("{Count} اعلان برای کاربر {UserId} خوانده شد.", count, userId);
        return count;
    }

    public async Task DeleteNotificationAsync(int id, int userId)
    {
        var notification = await _notificationRepository.GetByIdAsync(id);
        if (notification == null)
        {
            throw new NotFoundException("اعلان", id);
        }

        if (notification.UserId != userId)
        {
            throw new ForbiddenException("شما دسترسی لازم برای حذف این اعلان را ندارید.");
        }

        await _notificationRepository.DeleteAsync(id);
        await _notificationRepository.SaveChangesAsync();
        _logger.LogInformation("اعلان {NotificationId} توسط کاربر {UserId} حذف شد.", id, userId);
    }
}
