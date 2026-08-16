using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Application.Services;
using TicketHub.Core.Enums;
using TicketHub.Web.Components.Shared;
using Xunit;

namespace TicketHub.Tests.bUnit;

public class NotificationBellComponentTests : BUnitComponentTestBase
{
    private readonly Mock<INotificationService> _mockNotificationService = new();
    private readonly ITicketEventBroker _eventBroker = new TicketEventBroker();
    private readonly Mock<IToastService> _mockToastService = new();

    public NotificationBellComponentTests()
    {
        var authContext = this.AddAuthorization();
        authContext.SetAuthorized("TestUser");
        authContext.SetClaims(
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Name, "TestUser"),
            new Claim(ClaimTypes.Role, "Admin")
        );

        Services.AddSingleton(_mockNotificationService.Object);
        Services.AddSingleton(_eventBroker);
        Services.AddSingleton(_mockToastService.Object);
    }

    [Fact]
    public void NotificationBell_ShouldRenderUnreadBadge_WhenUnreadCountExists()
    {
        // Arrange
        _mockNotificationService.Setup(s => s.GetUnreadCountAsync(1)).ReturnsAsync(3);
        _mockNotificationService.Setup(s => s.GetSummaryAsync(1, 30)).ReturnsAsync(new NotificationSummaryDto
        {
            UnreadCount = 3,
            RecentNotifications = new List<NotificationDto>
            {
                new() { Id = 1, Title = "تیکت جدید", Message = "پیام تیکت", IsRead = false, CreatedAt = DateTime.UtcNow }
            }
        });

        // Act
        var cut = Render<NotificationBell>();

        // Assert
        var badge = cut.Find("span.bg-rose-500");
        badge.Should().NotBeNull(); badge.TextContent.Should().Contain("3");
    }

    [Fact]
    public async Task NotificationBell_ClickingBell_ShouldOpenDropdown_AndShowNotifications()
    {
        // Arrange
        _mockNotificationService.Setup(s => s.GetUnreadCountAsync(1)).ReturnsAsync(1);
        _mockNotificationService.Setup(s => s.GetSummaryAsync(1, 30)).ReturnsAsync(new NotificationSummaryDto
        {
            UnreadCount = 1,
            RecentNotifications = new List<NotificationDto>
            {
                new() { Id = 10, Title = "پاسخ جدید کارشناس", Message = "مشکل شما بررسی شد.", IsRead = false, CreatedAt = DateTime.UtcNow }
            }
        });

        var cut = Render<NotificationBell>();

        // Act - Click bell button
        var bellBtn = cut.Find("button[title='اعلانات']");
        await bellBtn.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert - Dropdown is rendered
        cut.Markup.Should().Contain("مرکز اعلانات"); cut.Markup.Should().Contain("پاسخ جدید کارشناس"); cut.Markup.Should().Contain("مشکل شما بررسی شد.");
    }

    [Fact]
    public async Task NotificationBell_RealtimeEvent_ShouldDynamicallyIncreaseBadge()
    {
        // Arrange
        _mockNotificationService.Setup(s => s.GetUnreadCountAsync(1)).ReturnsAsync(0);
        _mockNotificationService.Setup(s => s.GetSummaryAsync(1, 30)).ReturnsAsync(new NotificationSummaryDto
        {
            UnreadCount = 0,
            RecentNotifications = new List<NotificationDto>()
        });

        var cut = Render<NotificationBell>();

        // Initial check: no badge
        cut.FindAll("span.bg-rose-500").Should().BeEmpty();
        // Act - Trigger Real-Time Notification via EventBroker for User 1
        var newNotif = new NotificationDto
        {
            Id = 99,
            UserId = 1,
            Title = "رویداد زنده",
            Message = "یک اعلان ریل تایم دریافت شد",
            Type = NotificationType.TicketComment,
            Severity = NotificationSeverity.Info,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        await _eventBroker.PublishNotificationAsync(1, newNotif);

        // Assert - Badge should now appear with count 1
        var badge = cut.Find("span.bg-rose-500");
        badge.Should().NotBeNull(); badge.TextContent.Should().Contain("1");
    }

    [Fact]
    public async Task NotificationBell_MarkAllAsRead_ShouldCallService_AndClearBadge()
    {
        // Arrange
        _mockNotificationService.Setup(s => s.GetUnreadCountAsync(1)).ReturnsAsync(2);
        _mockNotificationService.Setup(s => s.GetSummaryAsync(1, 30)).ReturnsAsync(new NotificationSummaryDto
        {
            UnreadCount = 2,
            RecentNotifications = new List<NotificationDto>
            {
                new() { Id = 1, Title = "N1", Message = "M1", IsRead = false, CreatedAt = DateTime.UtcNow },
                new() { Id = 2, Title = "N2", Message = "M2", IsRead = false, CreatedAt = DateTime.UtcNow }
            }
        });
        _mockNotificationService.Setup(s => s.MarkAllAsReadAsync(1)).ReturnsAsync(2);

        var cut = Render<NotificationBell>();

        // Open dropdown
        await cut.Find("button[title='اعلانات']").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Click "خوانده شدن همه"
        var markAllBtn = cut.Find("button:contains('خوانده شدن همه')");
        await markAllBtn.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert - Service called
        _mockNotificationService.Verify(s => s.MarkAllAsReadAsync(1), Times.Once);
        cut.FindAll("span.bg-rose-500").Should().BeEmpty();
    }

    [Fact]
    public async Task NotificationBell_ClickingNotificationItem_ShouldOpenDetailsModal()
    {
        // Arrange
        var testNotif = new NotificationDto
        {
            Id = 42,
            UserId = 1,
            Title = "عنوان تست مودال",
            Message = "شرح پیام تست برای باز شدن مودال",
            Type = NotificationType.TicketComment,
            Severity = NotificationSeverity.Success,
            IsRead = false,
            ActionUrl = "/tickets/42",
            CreatedAt = DateTime.UtcNow
        };

        _mockNotificationService.Setup(s => s.GetUnreadCountAsync(1)).ReturnsAsync(1);
        _mockNotificationService.Setup(s => s.GetSummaryAsync(1, 30)).ReturnsAsync(new NotificationSummaryDto
        {
            UnreadCount = 1,
            RecentNotifications = new List<NotificationDto> { testNotif }
        });
        _mockNotificationService.Setup(s => s.MarkAsReadAsync(42, 1)).ReturnsAsync(true);

        var cut = Render<NotificationBell>();

        // Open dropdown
        await cut.Find("button[title='اعلانات']").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Click on notification item
        var itemDiv = cut.Find("div.notification-item");
        await itemDiv.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert - Modal is opened with details and MarkAsRead was called
        _mockNotificationService.Verify(s => s.MarkAsReadAsync(42, 1), Times.Once);
        var modalMessage = cut.Find("p.text-sm");
        modalMessage.TextContent.Should().Contain("شرح پیام تست برای باز شدن مودال"); var actionBtn = cut.Find("button:contains('مشاهده تیکت')");
        actionBtn.Should().NotBeNull();
    }
}

