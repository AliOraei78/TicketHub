using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using TicketHub.Application.DTOs;
using TicketHub.Application.Enums;
using TicketHub.Application.Interfaces;
using TicketHub.Application.Services;
using TicketHub.Core.Common.Exceptions;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;
using Xunit;
using ValidationException = TicketHub.Core.Common.Exceptions.ValidationException;

namespace TicketHub.Tests.bUnit
{
    public class TicketServiceTests
    {
        private readonly Mock<ITicketRepository> _mockTicketRepo;
        private readonly Mock<ILogger<TicketService>> _mockLogger;
        private readonly Mock<IValidator<TicketDto>> _mockValidator;
        private readonly Mock<IProjectRepository> _mockProjectRepo;
        private readonly Mock<IFileStorageService> _mockFileStorage;
        private readonly Mock<IWorkflowRepository> _mockWorkflowRepo;
        private readonly Mock<IHttpContextAccessor> _mockHttpAccessor;
        private readonly Mock<IPermissionService> _mockPermissionService;
        private readonly Mock<IRepository<TicketHistory>> _mockHistoryRepo;
        private readonly Mock<IRepository<Attachment>> _mockAttachmentRepo;
        private readonly Mock<IRepository<Role>> _mockRoleRepo;
        private readonly Mock<ITicketEventBroker> _mockEventBroker;
        private readonly Mock<IWorkflowAutomationService> _mockWorkflowAutomation;
        private readonly Mock<INotificationService> _mockNotificationService;

        private readonly TicketService _ticketService;

        public TicketServiceTests()
        {
            _mockTicketRepo = new Mock<ITicketRepository>();
            _mockLogger = new Mock<ILogger<TicketService>>();
            _mockValidator = new Mock<IValidator<TicketDto>>();
            _mockProjectRepo = new Mock<IProjectRepository>();
            _mockFileStorage = new Mock<IFileStorageService>();
            _mockWorkflowRepo = new Mock<IWorkflowRepository>();
            _mockHttpAccessor = new Mock<IHttpContextAccessor>();
            _mockPermissionService = new Mock<IPermissionService>();
            _mockHistoryRepo = new Mock<IRepository<TicketHistory>>();
            _mockAttachmentRepo = new Mock<IRepository<Attachment>>();
            _mockRoleRepo = new Mock<IRepository<Role>>();
            _mockEventBroker = new Mock<ITicketEventBroker>();
            _mockWorkflowAutomation = new Mock<IWorkflowAutomationService>();
            _mockNotificationService = new Mock<INotificationService>();

            // Setup Admin User by default
            var adminUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Name, "ادمین سیستم"),
                new Claim(ClaimTypes.Role, "ادمین")
            }, "TestAuth"));

            var httpContext = new DefaultHttpContext { User = adminUser };
            _mockHttpAccessor.Setup(h => h.HttpContext).Returns(httpContext);

            _mockPermissionService.Setup(p => p.HasAccessAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>(), PermissionType.Full))
                .ReturnsAsync(true);
            _mockPermissionService.Setup(p => p.HasAccessAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>(), PermissionType.SystemSection))
                .ReturnsAsync(true);

            _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<TicketDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _ticketService = new TicketService(
                _mockTicketRepo.Object,
                _mockLogger.Object,
                _mockValidator.Object,
                _mockProjectRepo.Object,
                _mockFileStorage.Object,
                _mockWorkflowRepo.Object,
                _mockHttpAccessor.Object,
                _mockPermissionService.Object,
                _mockHistoryRepo.Object,
                _mockAttachmentRepo.Object,
                _mockRoleRepo.Object,
                _mockEventBroker.Object,
                _mockWorkflowAutomation.Object,
                _mockNotificationService.Object
            );
        }

        [Fact]
        public async Task CreateAsync_WhenValidationFails_ThrowsValidationException()
        {
            _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<TicketDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(new[]
                {
                    new ValidationFailure("Title", "عنوان تیکت الزامی است.")
                }));

            var dto = new TicketDto { Title = "" };

            var act = () => _ticketService.CreateAsync(dto);
            await act.Should().ThrowAsync<ValidationException>();
            _mockTicketRepo.Verify(r => r.AddAsync(It.IsAny<Ticket>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_ValidTicket_AddsToRepositoryAndPublishesEvents()
        {
            var workflow = new Workflow
            {
                Id = 1,
                Name = "جریان عمومی",
                IsActive = true,
                WorkflowStatuses = new List<WorkflowStatus>
                {
                    new() { Id = 10, StatusId = 1, IsInitial = true }
                }
            };
            var project = new Project
            {
                Id = 1,
                Name = "پروژه عمومی",
                WorkflowId = 1,
                Workflow = workflow
            };

            _mockProjectRepo.Setup(p => p.GetProjectWithWorkflowAsync(1)).ReturnsAsync(project);
            _mockTicketRepo.Setup(r => r.AddAsync(It.IsAny<Ticket>())).Returns(Task.CompletedTask);

            var dto = new TicketDto
            {
                Title = "تیکت تستی جدید",
                Description = "شرح تیکت",
                ProjectId = 1,
                CategoryId = 10,
                StatusId = 1,
                PriorityId = 1,
                UserId = 1
            };

            await _ticketService.CreateAsync(dto);

            _mockTicketRepo.Verify(r => r.AddAsync(It.IsAny<Ticket>()), Times.Once);
            _mockEventBroker.Verify(e => e.PublishTicketUpdatedAsync(It.IsAny<int>()), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistentTicket_ThrowsNotFoundException()
        {
            _mockTicketRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Ticket?)null);

            var act = () => _ticketService.GetByIdAsync(999);
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task GetByIdAsync_CustomerViewingOtherUserTicket_ThrowsForbiddenException()
        {
            // Setup Customer Context (UserId = 2, non-admin, non-staff)
            var customerUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "2"),
                new Claim(ClaimTypes.Name, "مشتری"),
                new Claim(ClaimTypes.Role, "مشتری")
            }, "TestAuth"));
            _mockHttpAccessor.Setup(h => h.HttpContext).Returns(new DefaultHttpContext { User = customerUser });

            _mockPermissionService.Setup(p => p.HasAccessAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>(), PermissionType.Full))
                .ReturnsAsync(false);
            _mockPermissionService.Setup(p => p.HasAccessAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>(), PermissionType.SystemSection))
                .ReturnsAsync(false);

            // Ticket owned by user 1
            var ticket = new Ticket
            {
                Id = 55,
                Title = "تیکت کاربر ۱",
                UserId = 1
            };
            _mockTicketRepo.Setup(r => r.GetByIdAsync(55)).ReturnsAsync(ticket);

            var act = () => _ticketService.GetByIdAsync(55);
            await act.Should().ThrowAsync<ForbiddenException>();
        }

        [Fact]
        public async Task UpdateAsync_ValidDto_UpdatesRepositoryAndPublishesEvent()
        {
            var existingTicket = new Ticket { Id = 42, Title = "عنوان قدیم", Description = "شرح قدیم", UserId = 1 };
            _mockTicketRepo.Setup(r => r.GetByIdAsync(42)).ReturnsAsync(existingTicket);

            var dto = new TicketDto { Id = 42, Title = "عنوان جدید", Description = "شرح جدید", UserId = 1, ProjectId = 1, CategoryId = 1 };

            await _ticketService.UpdateAsync(dto);

            _mockTicketRepo.Verify(r => r.UpdateAsync(It.Is<Ticket>(t => t.Id == 42 && t.Title == "عنوان جدید")), Times.Once);
            _mockEventBroker.Verify(e => e.PublishTicketUpdatedAsync(42), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_NonExistentTicket_ThrowsNotFoundException()
        {
            _mockTicketRepo.Setup(r => r.GetByIdAsync(404)).ReturnsAsync((Ticket?)null);

            var act = () => _ticketService.DeleteAsync(404);
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task DeleteAsync_ExistingTicket_DeletesFilesAndCascadeDeletes()
        {
            var ticket = new Ticket
            {
                Id = 70,
                Title = "تیکت برای حذف",
                UserId = 1,
                Attachments = new List<Attachment>
                {
                    new() { Id = 1, FilePath = "/uploads/test1.pdf" }
                }
            };
            _mockTicketRepo.Setup(r => r.GetByIdAsync(70)).ReturnsAsync(ticket);

            await _ticketService.DeleteAsync(70);

            _mockFileStorage.Verify(f => f.DeleteFile("/uploads/test1.pdf"), Times.Once);
            _mockTicketRepo.Verify(r => r.DeleteAsync(70), Times.Once);
            _mockEventBroker.Verify(e => e.PublishTicketUpdatedAsync(70), Times.Once);
        }

        [Fact]
        public async Task DeleteRangeAsync_DeletesMultipleTicketsAndCleansFiles()
        {
            var tickets = new List<Ticket>
            {
                new() { Id = 1, Title = "تیکت ۱", Attachments = new List<Attachment> { new() { FilePath = "/uploads/1.png" } } },
                new() { Id = 2, Title = "تیکت ۲", Attachments = new List<Attachment> { new() { FilePath = "/uploads/2.png" } } }
            };
            _mockTicketRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(tickets[0]);
            _mockTicketRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(tickets[1]);

            await _ticketService.DeleteRangeAsync(new[] { 1, 2 });

            _mockFileStorage.Verify(f => f.DeleteFile("/uploads/1.png"), Times.Once);
            _mockFileStorage.Verify(f => f.DeleteFile("/uploads/2.png"), Times.Once);
            _mockTicketRepo.Verify(r => r.DeleteAsync(1), Times.Once);
            _mockTicketRepo.Verify(r => r.DeleteAsync(2), Times.Once);
        }

        [Fact]
        public async Task ExecuteTransitionAsync_ValidTransition_UpdatesTicketStatusAndPublishesEvents()
        {
            var ticket = new Ticket
            {
                Id = 30,
                Title = "تیکت تغییر وضعیت",
                StatusId = 1,
                WorkflowStatusId = 10,
                ProjectId = 1,
                UserId = 1,
                Project = new Project
                {
                    Id = 1,
                    WorkflowId = 1,
                    Workflow = new Workflow
                    {
                        Id = 1,
                        IsActive = true
                    }
                }
            };

            var transition = new Transition
            {
                Id = 201,
                Name = "تأیید و ارسال به مرحله بعد",
                FromState = 10,
                ToState = 20,
                IsActive = true,
                DeadlineMinutes = 120,
                ToStatus = new WorkflowStatus { Id = 20, StatusId = 2, Status = new Status { Id = 2, Name = "در حال اقدام" } },
                TransitionFields = new List<TransitionField>()
            };

            _mockTicketRepo.Setup(r => r.GetTicketWithProjectAndStatusAsync(30)).ReturnsAsync(ticket);
            _mockWorkflowRepo.Setup(w => w.GetTransitionWithDetailsAsync(201)).ReturnsAsync(transition);
            _mockTicketRepo.Setup(r => r.GetByIdAsync(30)).ReturnsAsync(ticket);

            var executeDto = new ExecuteTransitionDto
            {
                TicketId = 30,
                TransitionId = 201,
                Comment = "توضیحات انجام ترنزیشن"
            };

            await _ticketService.ExecuteTransitionAsync(executeDto, 1);

            _mockTicketRepo.Verify(r => r.ApplyTransitionAndSaveHistoryAsync(30, 2, 20, It.IsAny<TicketHistory>()), Times.Once);
            _mockEventBroker.Verify(e => e.PublishTransitionOccurredAsync(30), Times.Once);
        }

        [Fact]
        public async Task ExecuteTransitionAsync_InactiveTransition_ThrowsValidationException()
        {
            var ticket = new Ticket
            {
                Id = 31,
                Title = "تیکت غیرفعال",
                StatusId = 1,
                WorkflowStatusId = 10,
                ProjectId = 1,
                Project = new Project
                {
                    Id = 1,
                    WorkflowId = 1
                }
            };

            var transition = new Transition
            {
                Id = 202,
                Name = "انتقال غیرفعال",
                FromState = 10,
                ToState = 20,
                IsActive = false // INACTIVE
            };

            _mockTicketRepo.Setup(r => r.GetTicketWithProjectAndStatusAsync(31)).ReturnsAsync(ticket);
            _mockWorkflowRepo.Setup(w => w.GetTransitionWithDetailsAsync(202)).ReturnsAsync(transition);

            var executeDto = new ExecuteTransitionDto { TicketId = 31, TransitionId = 202 };

            var act = () => _ticketService.ExecuteTransitionAsync(executeDto, 1);
            await act.Should().ThrowAsync<ValidationException>();
        }

        [Fact]
        public async Task GetTicketTelemetrySummaryAsync_ReturnsAggregatedMetrics()
        {
            _mockTicketRepo.Setup(r => r.GetTicketTelemetryCountsAsync(null, null, null, null, null, 1, It.IsAny<List<int>?>(), true, true))
                .ReturnsAsync((3, 1, 1, 1, 1, 1, 0));

            var summary = await _ticketService.GetTicketTelemetrySummaryAsync();

            summary.Should().NotBeNull();
            summary.TotalTickets.Should().Be(3);
            summary.NewTicketsCount.Should().Be(1);
            summary.InProgressCount.Should().Be(1);
            summary.ResolvedCount.Should().Be(1);
            summary.CriticalCount.Should().Be(1);
            summary.OverdueCount.Should().Be(1);
        }
    }
}
