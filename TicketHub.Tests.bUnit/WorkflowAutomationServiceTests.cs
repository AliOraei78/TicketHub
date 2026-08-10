using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TicketHub.Application.Interfaces;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;
using TicketHub.Infrastructure.Data;
using TicketHub.Infrastructure.Services;
using Xunit;

namespace TicketHub.Tests.bUnit
{
    public class WorkflowAutomationServiceTests
    {
        private readonly Mock<ITicketRepository> _mockTicketRepo;
        private readonly Mock<ITicketEventBroker> _mockEventBroker;
        private readonly Mock<ILogger<WorkflowAutomationService>> _mockLogger;

        public WorkflowAutomationServiceTests()
        {
            _mockTicketRepo = new Mock<ITicketRepository>();
            _mockEventBroker = new Mock<ITicketEventBroker>();
            _mockLogger = new Mock<ILogger<WorkflowAutomationService>>();

            _mockEventBroker.Setup(b => b.PublishTransitionOccurredAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
            _mockEventBroker.Setup(b => b.PublishTicketUpdatedAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
            _mockTicketRepo.Setup(r => r.ApplyTransitionAndSaveHistoryAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<TicketHistory>())).Returns(Task.CompletedTask);
        }

        private IDbContextFactory<AppDbContext> CreateInMemoryDbContextFactory(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            var mockFactory = new Mock<IDbContextFactory<AppDbContext>>();
            mockFactory.Setup(f => f.CreateDbContextAsync(default))
                .ReturnsAsync(() => new AppDbContext(options));
            mockFactory.Setup(f => f.CreateDbContext())
                .Returns(() => new AppDbContext(options));

            return mockFactory.Object;
        }

        [Fact]
        public async Task ProcessAutomaticTransitionsAsync_ExecutesAutomatedTransition_WhenDue()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            var factory = CreateInMemoryDbContextFactory(dbName);

            using (var context = await factory.CreateDbContextAsync())
            {
                var user = new User { Id = 1, Name = "testuser", Email = "test@example.com" };
                await context.Set<User>().AddAsync(user);

                var s1 = new Status { Id = 100, Name = "Open" };
                var s2 = new Status { Id = 200, Name = "Resolved" };
                await context.Set<Status>().AddRangeAsync(s1, s2);

                var workflow = new Workflow
                {
                    Id = 1,
                    Name = "Auto WF",
                    IsActive = true
                };

                var status1 = new WorkflowStatus { Id = 10, NodeId = Guid.NewGuid(), StatusId = 100, Status = s1, WorkflowId = 1 };
                var status2 = new WorkflowStatus { Id = 20, NodeId = Guid.NewGuid(), StatusId = 200, Status = s2, WorkflowId = 1 };

                var transition = new Transition
                {
                    Id = 5,
                    Name = "Auto Move",
                    FromState = 10,
                    ToState = 20,
                    FromStatus = status1,
                    ToStatus = status2,
                    IsAutomated = 1,
                    IsActive = true,
                    ActivateAt = DateTime.UtcNow.AddMinutes(-5), // Due
                    WorkflowId = 1,
                    Workflow = workflow
                };

                workflow.WorkflowStatuses.Add(status1);
                workflow.WorkflowStatuses.Add(status2);
                workflow.Transitions.Add(transition);

                var project = new Project { Id = 1, Name = "P1", WorkflowId = 1, Workflow = workflow };
                var ticket = new Ticket
                {
                    Id = 42,
                    Title = "Test Ticket",
                    UserId = 1,
                    User = user,
                    ProjectId = 1,
                    Project = project,
                    WorkflowStatusId = 10,
                    StatusId = 100,
                    Status = s1
                };

                await context.Set<Workflow>().AddAsync(workflow);
                await context.Set<Project>().AddAsync(project);
                await context.Set<Ticket>().AddAsync(ticket);
                await context.SaveChangesAsync();
            }

            var service = new WorkflowAutomationService(factory, _mockTicketRepo.Object, _mockEventBroker.Object, _mockLogger.Object);

            // Act
            await service.ProcessAutomaticTransitionsAsync();

            // Assert
            _mockTicketRepo.Verify(r => r.ApplyTransitionAndSaveHistoryAsync(42, 200, 20, It.IsAny<TicketHistory>()), Times.Once);
            _mockEventBroker.Verify(b => b.PublishTransitionOccurredAsync(42), Times.Once);
            _mockEventBroker.Verify(b => b.PublishTicketUpdatedAsync(42), Times.Once);
        }

        [Fact]
        public async Task ProcessAutomaticTransitionsAsync_SkipsTransition_WhenActivateAtIsInFuture()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            var factory = CreateInMemoryDbContextFactory(dbName);

            using (var context = await factory.CreateDbContextAsync())
            {
                var s1 = new Status { Id = 101, Name = "Open" };
                var s2 = new Status { Id = 201, Name = "Resolved" };
                await context.Set<Status>().AddRangeAsync(s1, s2);

                var workflow = new Workflow { Id = 2, Name = "Future WF", IsActive = true };
                var status1 = new WorkflowStatus { Id = 11, NodeId = Guid.NewGuid(), StatusId = 101, Status = s1, WorkflowId = 2 };
                var status2 = new WorkflowStatus { Id = 21, NodeId = Guid.NewGuid(), StatusId = 201, Status = s2, WorkflowId = 2 };

                var transition = new Transition
                {
                    Id = 6,
                    Name = "Future Move",
                    FromState = 11,
                    ToState = 21,
                    FromStatus = status1,
                    ToStatus = status2,
                    IsAutomated = 1,
                    IsActive = true,
                    ActivateAt = DateTime.UtcNow.AddHours(2), // In future!
                    WorkflowId = 2,
                    Workflow = workflow
                };

                workflow.WorkflowStatuses.Add(status1);
                workflow.WorkflowStatuses.Add(status2);
                workflow.Transitions.Add(transition);

                var project = new Project { Id = 2, Name = "P2", WorkflowId = 2, Workflow = workflow };
                var ticket = new Ticket
                {
                    Id = 43,
                    Title = "Future Ticket",
                    ProjectId = 2,
                    Project = project,
                    WorkflowStatusId = 11,
                    StatusId = 101,
                    Status = s1
                };

                await context.Set<Workflow>().AddAsync(workflow);
                await context.Set<Project>().AddAsync(project);
                await context.Set<Ticket>().AddAsync(ticket);
                await context.SaveChangesAsync();
            }

            var service = new WorkflowAutomationService(factory, _mockTicketRepo.Object, _mockEventBroker.Object, _mockLogger.Object);

            // Act
            await service.ProcessAutomaticTransitionsAsync();

            // Assert
            _mockTicketRepo.Verify(r => r.ApplyTransitionAndSaveHistoryAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<TicketHistory>()), Times.Never);
        }

        [Fact]
        public async Task ProcessDeadlinesAsync_PublishesUpdate_ForOverdueTickets()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            var factory = CreateInMemoryDbContextFactory(dbName);

            using (var context = await factory.CreateDbContextAsync())
            {
                var user = new User { Id = 2, Name = "user2", Email = "u2@example.com" };
                var project = new Project { Id = 2, Name = "P2" };
                var s1 = new Status { Id = 300, Name = "In Progress" };
                await context.Set<User>().AddAsync(user);
                await context.Set<Project>().AddAsync(project);
                await context.Set<Status>().AddAsync(s1);

                var overdueTicket = new Ticket
                {
                    Id = 99,
                    Title = "Overdue Ticket",
                    UserId = 2,
                    User = user,
                    ProjectId = 2,
                    Project = project,
                    StatusId = 300,
                    Status = s1,
                    DueDate = DateTime.UtcNow.AddMinutes(-30) // Overdue
                };

                await context.Set<Ticket>().AddAsync(overdueTicket);
                await context.SaveChangesAsync();
            }

            var service = new WorkflowAutomationService(factory, _mockTicketRepo.Object, _mockEventBroker.Object, _mockLogger.Object);

            // Act
            await service.ProcessDeadlinesAsync();

            // Assert
            _mockEventBroker.Verify(b => b.PublishTicketUpdatedAsync(99), Times.Once);
        }
    }
}
