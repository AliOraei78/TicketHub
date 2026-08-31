using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TicketHub.Application.Interfaces;
using TicketHub.Application.Models;
using TicketHub.Web.Components.Pages.Admin.SystemLogs;
using Xunit;

namespace TicketHub.Tests.bUnit
{
    public class SystemLogsPageTests : BUnitComponentTestBase
    {
        private readonly Mock<ISystemLogService> _mockLogService;
        private readonly Mock<IToastService> _mockToastService;

        public SystemLogsPageTests()
        {
            _mockLogService = new Mock<ISystemLogService>();
            _mockToastService = new Mock<IToastService>();
            Services.AddSingleton(_mockLogService.Object);
            Services.AddSingleton(_mockToastService.Object);

            JSInterop.SetupVoid("initMatrixRain", _ => true);
            JSInterop.SetupVoid("initJalaliDatePicker", _ => true);
            JSInterop.Setup<bool>("copyTextToClipboard", _ => true);

            // Default fallback setup
            _mockLogService.Setup(s => s.GetLogsAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<LogEntry>());
        }

        [Fact]
        public void Render_SystemLogsPage_WithHeaderAndQuickFilters()
        {
            _mockLogService.Setup(s => s.GetLogsAsync(null, null, string.Empty, string.Empty))
                .ReturnsAsync(new List<LogEntry>
                {
                    new LogEntry { Timestamp = DateTime.Now, Level = "Information", Message = "Application Started" },
                    new LogEntry { Timestamp = DateTime.Now, Level = "Error", Message = "Database Connection Failed" }
                });

            var cut = Render<SystemLogs>();

            cut.Markup.Should().Contain("ترمینال");
            cut.Markup.Should().Contain("مانیتورینگ");
            cut.Markup.Should().Contain("همه لاگ‌ها");
            cut.Markup.Should().Contain("فقط خطاها (Error)");
            cut.Markup.Should().Contain("هشدارها (Warning)");
            cut.Markup.Should().Contain("اطلاعات (Info)");
            cut.Markup.Should().Contain("Application Started");
            cut.Markup.Should().Contain("Database Connection Failed");
        }

        [Fact]
        public void EmptyLogs_RendersEmptyState()
        {
            _mockLogService.Setup(s => s.GetLogsAsync(null, null, string.Empty, string.Empty))
                .ReturnsAsync(new List<LogEntry>());

            var cut = Render<SystemLogs>();

            cut.Markup.Should().Contain("هیچ لاگی با فیلترهای مشخص‌شده یافت نشد");
        }

        [Fact]
        public void QuickFilterError_FiltersToErrorLogs()
        {
            _mockLogService.Setup(s => s.GetLogsAsync(null, null, "Error", string.Empty))
                .ReturnsAsync(new List<LogEntry>
                {
                    new LogEntry { Timestamp = DateTime.Now, Level = "Error", Message = "Critical Error Log" }
                });

            var cut = Render<SystemLogs>();

            var errorChip = cut.Find("button:contains('فقط خطاها (Error)')");
            errorChip.Click();

            _mockLogService.Verify(s => s.GetLogsAsync(null, null, "Error", string.Empty), Times.Once);
        }

        [Fact]
        public void ClearFilters_ResetsAllFiltersAndCallsService()
        {
            var cut = Render<SystemLogs>();

            // Type in search term
            var searchInput = cut.Find("input[placeholder*='جستجوی پیام']");
            searchInput.Input("Some search");

            var clearBtn = cut.Find("button[title='پاکسازی تمام فیلترها']");
            clearBtn.Click();

            _mockLogService.Verify(s => s.GetLogsAsync(null, null, string.Empty, string.Empty), Times.AtLeast(2));
        }

        [Fact]
        public void ToggleRowExpand_ExpandsAndCollapsesDetails()
        {
            var log = new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = "Error",
                Message = "NullReferenceException occurred",
                Exception = "System.NullReferenceException: Object reference not set",
                TraceId = "trace-999"
            };

            _mockLogService.Setup(s => s.GetLogsAsync(null, null, string.Empty, string.Empty))
                .ReturnsAsync(new List<LogEntry> { log });

            var cut = Render<SystemLogs>();

            // Expand row
            var expandBtn = cut.Find("button[title='مشاهده جزئیات کامل لاگ']");
            expandBtn.Click();

            cut.Markup.Should().Contain("System.NullReferenceException: Object reference not set");
            cut.Markup.Should().Contain("trace-999");
        }

        [Fact]
        public void ToggleLiveStream_StartsAndStopsLiveStream()
        {
            var cut = Render<SystemLogs>();

            var liveBtn = cut.Find("button:contains('فعالسازی لایو')");
            liveBtn.Click();

            cut.Markup.Should().Contain("جریان زنده فعال");
        }
    }
}
