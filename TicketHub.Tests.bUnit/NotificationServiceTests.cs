using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Application.Services;
using TicketHub.Core.Entities;
using TicketHub.Core.Enums;
using TicketHub.Core.Interfaces;
using TicketHub.Infrastructure.Data;
using TicketHub.Infrastructure.Repositories;
using Xunit;

namespace TicketHub.Tests.bUnit;

public class NotificationServiceTests
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly INotificationRepository _notificationRepository;
    private readonly Mock<ITicketEventBroker> _mockEventBroker;
    private readonly INotificationService _notificationService;

    public NotificationServiceTests()
    {
        var dbName = "TestDb_Notification_" + Guid.NewGuid();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        var mockFactory = new Mock<IDbContextFactory<AppDbContext>>();
        mockFactory.Setup(f => f.CreateDbContextAsync(default))
                   .ReturnsAsync(() => new AppDbContext(options));

        _factory = mockFactory.Object;
        _notificationRepository = new NotificationRepository(_factory);
        _mockEventBroker = new Mock<ITicketEventBroker>();

        _notificationService = new NotificationService(
            _notificationRepository,
            _mockEventBroker.Object,
            NullLogger<NotificationService>.Instance);
    }

    [Fact]
    public async Task CreateNotificationAsync_ShouldSaveToDb_AndPublishRealtimeEvent()
    {
        // Arrange
        var createDto = new CreateNotificationDto
        {
            UserId = 1,
            Title = "پاسخ جدید",
            Message = "یک نظر جدید روی تیکت ثبت شد.",
            Type = NotificationType.TicketComment,
            Severity = NotificationSeverity.Info,
            ReferenceId = 100,
            ActionUrl = "/tickets/100"
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(createDto);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal("پاسخ جدید", result.Title);
        Assert.False(result.IsRead);

        // Verify Real-time broker was called
        _mockEventBroker.Verify(b => b.PublishNotificationAsync(1, It.Is<NotificationDto>(n => n.Id == result.Id)), Times.Once);
    }

    [Fact]
    public async Task GetUnreadCountAsync_ShouldReturnAccurateCountPerUser()
    {
        // Arrange
        await _notificationService.CreateNotificationAsync(new CreateNotificationDto { UserId = 1, Title = "N1", Message = "M1" });
        await _notificationService.CreateNotificationAsync(new CreateNotificationDto { UserId = 1, Title = "N2", Message = "M2" });
        await _notificationService.CreateNotificationAsync(new CreateNotificationDto { UserId = 2, Title = "N3", Message = "M3" });

        // Act
        var countUser1 = await _notificationService.GetUnreadCountAsync(1);
        var countUser2 = await _notificationService.GetUnreadCountAsync(2);
        var countUser3 = await _notificationService.GetUnreadCountAsync(3);

        // Assert
        Assert.Equal(2, countUser1);
        Assert.Equal(1, countUser2);
        Assert.Equal(0, countUser3);
    }

    [Fact]
    public async Task MarkAsReadAsync_ShouldMarkNotificationAsRead_ForCorrectUser()
    {
        // Arrange
        var n = await _notificationService.CreateNotificationAsync(new CreateNotificationDto { UserId = 1, Title = "N1", Message = "M1" });

        // Act - User 2 tries to mark User 1's notification (should fail)
        var failResult = await _notificationService.MarkAsReadAsync(n.Id, userId: 2);
        Assert.False(failResult);

        // Act - User 1 marks own notification
        var successResult = await _notificationService.MarkAsReadAsync(n.Id, userId: 1);
        Assert.True(successResult);

        var unread = await _notificationService.GetUnreadCountAsync(1);
        Assert.Equal(0, unread);
    }

    [Fact]
    public async Task MarkAllAsReadAsync_ShouldMarkAllUnreadForUser()
    {
        // Arrange
        await _notificationService.CreateNotificationAsync(new CreateNotificationDto { UserId = 1, Title = "N1", Message = "M1" });
        await _notificationService.CreateNotificationAsync(new CreateNotificationDto { UserId = 1, Title = "N2", Message = "M2" });
        await _notificationService.CreateNotificationAsync(new CreateNotificationDto { UserId = 2, Title = "N3", Message = "M3" });

        // Act
        var markedCount = await _notificationService.MarkAllAsReadAsync(1);

        // Assert
        Assert.Equal(2, markedCount);
        Assert.Equal(0, await _notificationService.GetUnreadCountAsync(1));
        Assert.Equal(1, await _notificationService.GetUnreadCountAsync(2)); // User 2 still has 1 unread
    }

    [Fact]
    public async Task DeleteNotificationAsync_ShouldDeleteOnlyForOwner()
    {
        // Arrange
        var n = await _notificationService.CreateNotificationAsync(new CreateNotificationDto { UserId = 1, Title = "N1", Message = "M1" });

        // Act & Assert - User 2 cannot delete
        await Assert.ThrowsAsync<TicketHub.Core.Common.Exceptions.ForbiddenException>(() =>
            _notificationService.DeleteNotificationAsync(n.Id, userId: 2));

        // Act - User 1 deletes
        await _notificationService.DeleteNotificationAsync(n.Id, userId: 1);
        var unread = await _notificationService.GetUnreadCountAsync(1);
        Assert.Equal(0, unread);
    }
}
