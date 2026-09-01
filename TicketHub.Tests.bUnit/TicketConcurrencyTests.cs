using System;
using System.IO;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Application.Services;
using TicketHub.Core.Common.Exceptions;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;
using TicketHub.Infrastructure.Data;
using TicketHub.Infrastructure.Repositories;
using TicketHub.Web.Middlewares;
using Xunit;

namespace TicketHub.Tests.bUnit
{
    public class TicketConcurrencyTests
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

        public TicketConcurrencyTests()
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

            _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<TicketDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Role, "ادمین")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);
            var httpContext = new DefaultHttpContext { User = principal };
            _mockHttpAccessor.Setup(h => h.HttpContext).Returns(httpContext);

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
                _mockNotificationService.Object);
        }

        [Fact]
        public async Task UpdateAsync_ThrowsConcurrencyException_WhenRowVersionMismatches()
        {
            // Arrange
            var serverVersion = Guid.NewGuid();
            var staleClientVersion = Guid.NewGuid();

            var ticketInDb = new Ticket
            {
                Id = 10,
                Title = "عنوان سرور",
                RowVersion = serverVersion,
                StatusId = 1,
                ProjectId = 1,
                UserId = 1
            };

            _mockTicketRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(ticketInDb);

            var updateDto = new TicketDto
            {
                Id = 10,
                Title = "ویرایش کلاینت",
                RowVersion = staleClientVersion,
                StatusId = 1,
                ProjectId = 1,
                UserId = 1
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ConcurrencyException>(() => _ticketService.UpdateAsync(updateDto));
            Assert.Contains("تغییر", ex.Message);
            Assert.Equal(409, ex.StatusCode);
            Assert.Equal("CONCURRENCY_CONFLICT", ex.ErrorCode);
        }

        [Fact]
        public async Task UpdateAsync_ThrowsConcurrencyException_WhenDbUpdateConcurrencyExceptionOccurs()
        {
            // Arrange
            var currentVersion = Guid.NewGuid();

            var ticketInDb = new Ticket
            {
                Id = 20,
                Title = "تیکت تستی",
                RowVersion = currentVersion,
                StatusId = 1,
                ProjectId = 1,
                UserId = 1
            };

            _mockTicketRepo.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(ticketInDb);
            _mockTicketRepo.Setup(r => r.UpdateAsync(It.IsAny<Ticket>()))
                .ThrowsAsync(new DbUpdateConcurrencyException("Row modified concurrently", new[] { Mock.Of<Microsoft.EntityFrameworkCore.Update.IUpdateEntry>() }));

            var updateDto = new TicketDto
            {
                Id = 20,
                Title = "تغییر عنوان",
                RowVersion = currentVersion,
                StatusId = 1,
                ProjectId = 1,
                UserId = 1
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ConcurrencyException>(() => _ticketService.UpdateAsync(updateDto));
            Assert.Equal(409, ex.StatusCode);
            Assert.Equal("CONCURRENCY_CONFLICT", ex.ErrorCode);
        }

        [Fact]
        public async Task ExecuteTransitionAsync_ThrowsConcurrencyException_WhenDbUpdateConcurrencyExceptionOccurs()
        {
            // Arrange
            var ticketInDb = new Ticket
            {
                Id = 30,
                Title = "تیکت ترنزیشن",
                StatusId = 1,
                WorkflowStatusId = 10,
                ProjectId = 1,
                RowVersion = Guid.NewGuid(),
                Project = new Project
                {
                    Id = 1,
                    WorkflowId = 100,
                    Workflow = new Workflow
                    {
                        Id = 100,
                        WorkflowStatuses = new[]
                        {
                            new WorkflowStatus { Id = 10, StatusId = 1 }
                        }
                    }
                }
            };

            var transition = new Transition
            {
                Id = 201,
                Name = "بررسی",
                FromState = 10,
                ToState = 20,
                IsActive = true,
                ToStatus = new WorkflowStatus { Id = 20, StatusId = 2, Status = new Status { Id = 2, Name = "در حال بررسی" } }
            };

            _mockTicketRepo.Setup(r => r.GetTicketWithProjectAndStatusAsync(30)).ReturnsAsync(ticketInDb);
            _mockWorkflowRepo.Setup(r => r.GetTransitionWithDetailsAsync(201)).ReturnsAsync(transition);
            _mockTicketRepo.Setup(r => r.ApplyTransitionAndSaveHistoryAsync(30, 2, 20, It.IsAny<TicketHistory>(), It.IsAny<Guid?>()))
                .ThrowsAsync(new DbUpdateConcurrencyException("Concurrency conflict"));

            var executeDto = new ExecuteTransitionDto
            {
                TicketId = 30,
                TransitionId = 201,
                RowVersion = ticketInDb.RowVersion
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ConcurrencyException>(() => _ticketService.ExecuteTransitionAsync(executeDto, 1));
            Assert.Equal(409, ex.StatusCode);
            Assert.Equal("CONCURRENCY_CONFLICT", ex.ErrorCode);
        }

        [Fact]
        public async Task ApplyTransitionAndSaveHistoryAsync_ThrowsConcurrencyException_WhenExpectedRowVersionMismatches()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            var mockFactory = new Mock<IDbContextFactory<AppDbContext>>();
            mockFactory.Setup(f => f.CreateDbContextAsync(default)).ReturnsAsync(() => new AppDbContext(options));

            var serverVersion = Guid.NewGuid();
            var staleClientVersion = Guid.NewGuid();

            using (var context = new AppDbContext(options))
            {
                context.Tickets.Add(new Ticket
                {
                    Id = 55,
                    Title = "تیکت تست مخزن",
                    StatusId = 1,
                    RowVersion = serverVersion,
                    UserId = 1,
                    ProjectId = 1
                });
                await context.SaveChangesAsync();
            }

            var repo = new TicketRepository(mockFactory.Object);
            var history = new TicketHistory
            {
                TicketId = 55,
                Comment = "انتقال با ورژن قدیمی"
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ConcurrencyException>(() =>
                repo.ApplyTransitionAndSaveHistoryAsync(55, 2, 20, history, staleClientVersion));

            Assert.Equal(409, ex.StatusCode);
            Assert.Equal("CONCURRENCY_CONFLICT", ex.ErrorCode);
        }

        [Fact]
        public async Task AppDbContext_RotatesRowVersion_OnTicketModification()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            Guid initialVersion;

            using (var context = new AppDbContext(options))
            {
                var ticket = new Ticket
                {
                    Id = 99,
                    Title = "تیکت اولیه",
                    StatusId = 1,
                    UserId = 1,
                    ProjectId = 1
                };
                context.Tickets.Add(ticket);
                await context.SaveChangesAsync();
                initialVersion = ticket.RowVersion;
            }

            // Act
            Guid updatedVersion;
            using (var context = new AppDbContext(options))
            {
                var ticket = await context.Tickets.FindAsync(99);
                Assert.NotNull(ticket);
                ticket.Title = "تیکت آپدیت شده";
                await context.SaveChangesAsync();
                updatedVersion = ticket.RowVersion;
            }

            // Assert
            Assert.NotEqual(Guid.Empty, initialVersion);
            Assert.NotEqual(Guid.Empty, updatedVersion);
            Assert.NotEqual(initialVersion, updatedVersion);
        }

        [Fact]
        public async Task GlobalExceptionHandler_Returns409Conflict_OnDbUpdateConcurrencyException()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<GlobalExceptionHandler>>();
            var handler = new GlobalExceptionHandler(loggerMock.Object);

            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            var exception = new DbUpdateConcurrencyException("Concurrency conflict in EF");

            // Act
            var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

            // Assert
            Assert.True(handled);
            Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(context.Response.Body);
            var responseBody = await reader.ReadToEndAsync();
            var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseBody);

            Assert.NotNull(problemDetails);
            Assert.Equal(409, problemDetails.Status);
            Assert.Equal("CONCURRENCY_CONFLICT", problemDetails.Extensions["errorCode"]?.ToString());
        }

        [Fact]
        public async Task GlobalExceptionHandler_Returns409Conflict_OnConcurrencyException()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<GlobalExceptionHandler>>();
            var handler = new GlobalExceptionHandler(loggerMock.Object);

            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            var exception = new ConcurrencyException("پیام خطای تداخل همزمانی");

            // Act
            var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

            // Assert
            Assert.True(handled);
            Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(context.Response.Body);
            var responseBody = await reader.ReadToEndAsync();
            var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseBody);

            Assert.NotNull(problemDetails);
            Assert.Equal(409, problemDetails.Status);
            Assert.Equal("CONCURRENCY_CONFLICT", problemDetails.Extensions["errorCode"]?.ToString());
        }
    }
}
