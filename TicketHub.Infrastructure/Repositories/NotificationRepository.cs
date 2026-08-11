using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;
using TicketHub.Infrastructure.Data;

namespace TicketHub.Infrastructure.Repositories;

public class NotificationRepository : GenericRepository<Notification>, INotificationRepository
{
    public NotificationRepository(IDbContextFactory<AppDbContext> factory) : base(factory)
    {
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.Notifications
            .AsNoTracking()
            .CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    public async Task<List<Notification>> GetUserNotificationsAsync(int userId, int page = 1, int pageSize = 20, bool? unreadOnly = null)
    {
        using var context = await _factory.CreateDbContextAsync();
        var query = context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId);

        if (unreadOnly.HasValue && unreadOnly.Value)
        {
            query = query.Where(n => !n.IsRead);
        }

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<bool> MarkAsReadAsync(int id, int userId)
    {
        using var context = await _factory.CreateDbContextAsync();
        var notification = await context.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
        if (notification == null)
        {
            return false;
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }

        return true;
    }

    public async Task<int> MarkAllAsReadAsync(int userId)
    {
        using var context = await _factory.CreateDbContextAsync();
        var unreadNotifications = await context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        if (!unreadNotifications.Any())
        {
            return 0;
        }

        var now = DateTime.UtcNow;
        foreach (var item in unreadNotifications)
        {
            item.IsRead = true;
            item.ReadAt = now;
        }

        await context.SaveChangesAsync();
        return unreadNotifications.Count;
    }
}
