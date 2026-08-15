using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Bunit;
using FluentValidation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TicketHub.Application.DTOs;
using TicketHub.Application.Enums;
using TicketHub.Application.Interfaces;
using TicketHub.Core.Common.Exceptions;
using TicketHub.Web.Components.Pages.Main.Tickets;
using Xunit;

namespace TicketHub.Tests.bUnit
{
    public class TicketDetailsTests : BUnitComponentTestBase
    {
        private readonly Mock<ITicketService> _mockTicketService;
        private readonly Mock<ICommentService> _mockCommentService;
        private readonly Mock<IStatusService> _mockStatusService;
        private readonly Mock<ICategoryService> _mockCategoryService;
        private readonly Mock<IPriorityService> _mockPriorityService;
        private readonly Mock<IToastService> _mockToastService;
        private readonly Mock<IPermissionService> _mockPermissionService;
        private readonly Mock<IWorkflowService> _mockWorkflowService;
        private readonly Mock<IRoleService> _mockRoleService;
        private readonly Mock<ITicketEventBroker> _mockEventBroker;
        private readonly Mock<AuthenticationStateProvider> _mockAuthStateProvider;

        private readonly TicketDto _sampleTicket;

        public TicketDetailsTests()
        {
            _mockTicketService = new Mock<ITicketService>();
            _mockCommentService = new Mock<ICommentService>();
            _mockStatusService = new Mock<IStatusService>();
            _mockCategoryService = new Mock<ICategoryService>();
            _mockPriorityService = new Mock<IPriorityService>();
            _mockToastService = new Mock<IToastService>();
            _mockPermissionService = new Mock<IPermissionService>();
            _mockWorkflowService = new Mock<IWorkflowService>();
            _mockRoleService = new Mock<IRoleService>();
            _mockEventBroker = new Mock<ITicketEventBroker>();
            _mockAuthStateProvider = new Mock<AuthenticationStateProvider>();

            Services.AddSingleton(_mockTicketService.Object);
            Services.AddSingleton(_mockCommentService.Object);
            Services.AddSingleton(_mockStatusService.Object);
            Services.AddSingleton(_mockCategoryService.Object);
            Services.AddSingleton(_mockPriorityService.Object);
            Services.AddSingleton(_mockToastService.Object);
            Services.AddSingleton(_mockPermissionService.Object);
            Services.AddSingleton(_mockWorkflowService.Object);
            Services.AddSingleton(_mockRoleService.Object);
            Services.AddSingleton(_mockEventBroker.Object);
            Services.AddSingleton(_mockAuthStateProvider.Object);
            Services.AddSingleton(Mock.Of<IValidator<ExecuteTransitionDto>>());
            Services.AddSingleton(Mock.Of<IValidator<TicketDto>>());

            // Default Claims
            var userClaims = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Name, "مدیر کل سیستم"),
                new Claim(ClaimTypes.Email, "admin@tickethub.io"),
                new Claim(ClaimTypes.Role, "ادمین")
            }, "TestAuth"));
            _mockAuthStateProvider.Setup(a => a.GetAuthenticationStateAsync())
                .ReturnsAsync(new AuthenticationState(userClaims));

            _mockPermissionService.Setup(p => p.HasAccessAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>(), It.IsAny<PermissionType>()))
                .ReturnsAsync(true);

            // Sample Data
            _sampleTicket = new TicketDto
            {
                Id = 42,
                Title = "خطای بارگذاری در مرحله دوم",
                Description = "توضیحات کامل خطای بارگذاری بازی در مرحله دوم",
                UserId = 1,
                User = new UserDto { Id = 1, Name = "مدیر کل سیستم", Email = "admin@tickethub.io" },
                ProjectId = 1,
                Project = new ProjectDto { Id = 1, Name = "پروژه عمومی", WorkflowId = 1 },
                CategoryId = 1,
                Category = new CategoryDto { Id = 1, Name = "باگ و خطا" },
                PriorityId = 1,
                Priority = new PriorityDto { Id = 1, Name = "بحرانی", Level = 4, ColorCode = "#ef4444" },
                StatusId = 1,
                Status = new StatusDto { Id = 1, Name = "جدید", ColorCode = "#38bdf8" },
                CreatedAt = DateTime.UtcNow.AddHours(-2),
                Attachments = new List<AttachmentDto>
                {
                    new() { Id = 101, FileName = "crash_log.txt", FilePath = "/uploads/crash_log.txt" }
                }
            };

            _mockTicketService.Setup(t => t.GetByIdAsync(42)).ReturnsAsync(_sampleTicket);
            _mockStatusService.Setup(s => s.GetAllAsync()).ReturnsAsync(new List<StatusDto>
            {
                new() { Id = 1, Name = "جدید", ColorCode = "#38bdf8" },
                new() { Id = 2, Name = "در حال بررسی", ColorCode = "#84cc16" }
            });
            _mockCategoryService.Setup(c => c.GetAllAsync()).ReturnsAsync(new List<CategoryDto>
            {
                new() { Id = 1, Name = "باگ و خطا" }
            });
            _mockPriorityService.Setup(p => p.GetAllAsync()).ReturnsAsync(new List<PriorityDto>
            {
                new() { Id = 1, Name = "بحرانی", Level = 4, ColorCode = "#ef4444" },
                new() { Id = 2, Name = "عادی", Level = 2, ColorCode = "#3b82f6" }
            });
            _mockCommentService.Setup(c => c.GetCommentsByTicketIdAsync(42)).ReturnsAsync(new List<CommentDto>
            {
                new() { Id = 1, TicketId = 42, UserId = 1, Content = "نظر اول تستی", CreatedAt = DateTime.UtcNow.AddMinutes(-30), User = new UserDto { Id = 1, Name = "مدیر کل سیستم" } }
            });
            _mockTicketService.Setup(t => t.GetTransitionsByTicketIdAsync(42)).ReturnsAsync(new List<TicketHistoryDto>
            {
                new() { Id = 1, TicketId = 42, FromStatusName = "جدید", ToStatusName = "در حال بررسی", TransitionTitle = "شروع بررسی", UserName = "مدیر کل سیستم", CreatedAt = DateTime.UtcNow.AddMinutes(-20), Comment = "شروع کار روی لاگ‌ها" }
            });
        }

        [Fact]
        public void TicketDetails_NotFoundOrNull_NavigatesToTicketsList()
        {
            _mockTicketService.Setup(t => t.GetByIdAsync(999)).ReturnsAsync((TicketDto?)null);

            var nav = Services.GetRequiredService<NavigationManager>();
            Render<TicketDetails>(parameters => parameters.Add(p => p.TicketId, 999));

            nav.Uri.Should().EndWith("/tickets");        }

        [Fact]
        public void TicketDetails_ForbiddenException_ShowsToastAndNavigatesToTicketsList()
        {
            _mockTicketService.Setup(t => t.GetByIdAsync(888)).ThrowsAsync(new ForbiddenException("عدم دسترسی به تیکت"));

            var nav = Services.GetRequiredService<NavigationManager>();
            Render<TicketDetails>(parameters => parameters.Add(p => p.TicketId, 888));

            _mockToastService.Verify(t => t.ShowWarning(It.Is<string>(s => s.Contains("عدم دسترسی")), "عدم دسترسی"), Times.Once);
            nav.Uri.Should().EndWith("/tickets");        }

        [Fact]
        public void TicketDetails_Renders3CardLayout_AndHeaderMetadataCorrectly()
        {
            var cut = Render<TicketDetails>(parameters => parameters.Add(p => p.TicketId, 42));

            // Header metadata
            cut.Markup.Should().Contain("#42");            cut.Markup.Should().Contain("پروژه: پروژه عمومی");            cut.Markup.Should().Contain("دسته: باگ و خطا");            cut.Markup.Should().Contain("خطای بارگذاری در مرحله دوم");            cut.Markup.Should().Contain("توضیحات کامل خطای بارگذاری بازی در مرحله دوم");
            // Card 1 (Attachments & Overview)
            cut.Markup.Should().Contain("crash_log.txt");
            // Card 2 (Comments Stream)
            cut.Markup.Should().Contain("نظرات و گفتگو");            cut.Markup.Should().Contain("نظر اول تستی");
            // Card 3 (Status & Transitions Timeline)
            cut.Markup.Should().Contain("وضعیت و انتقالات");            cut.Markup.Should().Contain("شروع بررسی");            cut.Markup.Should().Contain("شروع کار روی لاگ‌ها");        }

        [Fact]
        public void InlineEdit_Title_EnablesInput_AndSavesTitle()
        {
            var cut = Render<TicketDetails>(parameters => parameters.Add(p => p.TicketId, 42));

            // Click Edit Title button
            var editBtn = cut.Find("button[title='ویرایش عنوان']");
            editBtn.Click();

            // Find input and submit change
            var titleInput = cut.Find("input[type='text']");
            titleInput.Change("عنوان جدید ویرایش شده");

            var saveBtn = cut.Find("button[title='ذخیره عنوان']");
            saveBtn.Click();

            _mockTicketService.Verify(t => t.UpdateAsync(It.Is<TicketDto>(dto => dto.Id == 42 && dto.Title == "عنوان جدید ویرایش شده")), Times.Once);
        }

        [Fact]
        public void InlineEdit_Description_EnablesTextarea_AndSavesDescription()
        {
            var cut = Render<TicketDetails>(parameters => parameters.Add(p => p.TicketId, 42));

            // Click Edit Description button
            var editBtn = cut.Find("button[title='ویرایش شرح']");
            editBtn.Click();

            // Find textarea and change value
            var descArea = cut.Find("textarea");
            descArea.Change("شرح جدید و تکمیلی تیکت");

            var saveBtn = cut.Find("button:contains('ذخیره توضیحات')");
            saveBtn.Click();

            _mockTicketService.Verify(t => t.UpdateAsync(It.Is<TicketDto>(dto => dto.Id == 42 && dto.Description == "شرح جدید و تکمیلی تیکت")), Times.Once);
        }

        [Fact]
        public void PriorityDropdown_Change_CallsTicketServiceUpdate()
        {
            var cut = Render<TicketDetails>(parameters => parameters.Add(p => p.TicketId, 42));

            var prioritySelect = cut.Find("select");
            prioritySelect.Change("2");

            _mockTicketService.Verify(t => t.UpdateAsync(It.Is<TicketDto>(dto => dto.Id == 42 && dto.PriorityId == 2)), Times.Once);
        }

        [Fact]
        public void CommentsStream_CanAddNewComment()
        {
            var cut = Render<TicketDetails>(parameters => parameters.Add(p => p.TicketId, 42));

            var commentInput = cut.Find("input[placeholder='نظر خود را بنویسید...']");
            commentInput.Change("این یک نظر جدید است.");

            var submitBtn = cut.Find("button:contains('ارسال')");
            submitBtn.Click();

            _mockCommentService.Verify(c => c.AddCommentAsync(It.Is<CommentDto>(dto => dto.TicketId == 42 && dto.Content == "این یک نظر جدید است."), 1), Times.Once);
        }

        [Fact]
        public void CommentsStream_DeleteComment_CallsCommentServiceDelete()
        {
            var cut = Render<TicketDetails>(parameters => parameters.Add(p => p.TicketId, 42));

            var deleteBtn = cut.Find("button[title='حذف نظر']");
            deleteBtn.Click();

            _mockCommentService.Verify(c => c.DeleteCommentAsync(1, 1, true), Times.Once);
        }

        [Fact]
        public void Attachments_DeleteAttachment_CallsTicketServiceDeleteAttachment()
        {
            var cut = Render<TicketDetails>(parameters => parameters.Add(p => p.TicketId, 42));

            var deleteAttBtn = cut.Find("button[title='حذف فایل']");
            deleteAttBtn.Click();

            _mockTicketService.Verify(t => t.DeleteAttachmentAsync(101, 1, true), Times.Once);
        }

        [Fact]
        public void RealTimeEventBroker_RefreshesData_OnTicketAndCommentEvents()
        {
            var cut = Render<TicketDetails>(parameters => parameters.Add(p => p.TicketId, 42));

            // Trigger OnCommentAdded
            var newComment = new CommentDto { Id = 2, TicketId = 42, Content = "کامنت زنده دریافتی", User = new UserDto { Name = "پشتیبان" }, CreatedAt = DateTime.UtcNow };
            _mockEventBroker.Raise(e => e.OnCommentAdded += null, 42, newComment);
            cut.Markup.Should().Contain("کامنت زنده دریافتی");
            // Trigger OnCommentDeleted
            _mockEventBroker.Raise(e => e.OnCommentDeleted += null, 42, 1);
            cut.Markup.Should().NotContain("نظر اول تستی");
            // Trigger OnTransitionOccurred
            _mockEventBroker.Raise(e => e.OnTransitionOccurred += null, 42);
            _mockTicketService.Verify(t => t.GetTransitionsByTicketIdAsync(42), Times.AtLeast(2));
        }

        [Fact]
        public void GoBack_NavigatesToTickets()
        {
            var cut = Render<TicketDetails>(parameters => parameters.Add(p => p.TicketId, 42));
            var nav = Services.GetRequiredService<NavigationManager>();

            var backBtn = cut.Find("button:contains('بازگشت به لیست تیکت‌ها')");
            backBtn.Click();

            nav.Uri.Should().EndWith("/tickets");        }
    }
}
