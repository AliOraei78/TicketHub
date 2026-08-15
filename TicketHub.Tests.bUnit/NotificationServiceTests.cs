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
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.Title.Should().Be("پاسخ جدید");
        result.IsRead.Should().BeFalse();
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
        countUser1.Should().Be(2);        countUser2.Should().Be(1);        countUser3.Should().Be(0);    }

    [Fact]
    public async Task MarkAsReadAsync_ShouldMarkNotificationAsRead_ForCorrectUser()
    {
        // Arrange
        var n = await _notificationService.CreateNotificationAsync(new CreateNotificationDto { UserId = 1, Title = "N1", Message = "M1" });

        // Act - User 2 tries to mark User 1's notification (should fail)
        var failResult = await _notificationService.MarkAsReadAsync(n.Id, userId: 2);
        failResult.Should().BeFalse();
        // Act - User 1 marks own notification
        var successResult = await _notificationService.MarkAsReadAsync(n.Id, userId: 1);
        successResult.Should().BeTrue();
        var unread = await _notificationService.GetUnreadCountAsync(1);
        unread.Should().Be(0);
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
        markedCount.Should().Be(2);
        (await _notificationService.GetUnreadCountAsync(1)).Should().Be(0);
        (await _notificationService.GetUnreadCountAsync(2)).Should().Be(1); // User 2 still has 1 unread
    }

    [Fact]
    public async Task DeleteNotificationAsync_ShouldDeleteOnlyForOwner()
    {
        // Arrange
        var n = await _notificationService.CreateNotificationAsync(new CreateNotificationDto { UserId = 1, Title = "N1", Message = "M1" });

        // Act & Assert - User 2 cannot delete
        var act = () => _notificationService.DeleteNotificationAsync(n.Id, userId: 2);
        await act.Should().ThrowAsync<TicketHub.Core.Common.Exceptions.ForbiddenException>();

        // Act - User 1 deletes
        await _notificationService.DeleteNotificationAsync(n.Id, userId: 1);
        var unread = await _notificationService.GetUnreadCountAsync(1);
        unread.Should().Be(0);
    }
}
