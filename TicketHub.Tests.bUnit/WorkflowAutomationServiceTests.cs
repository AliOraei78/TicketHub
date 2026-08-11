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

        #region Automatic Transition Scenarios

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

                var workflow = new Workflow { Id = 1, Name = "Auto WF", IsActive = true };
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
                    ActivateAt = DateTime.UtcNow.AddMinutes(-5),
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
            _mockTicketRepo.Verify(r => r.ApplyTransitionAndSaveHistoryAsync(42, 200, 20, It.Is<TicketHistory>(h => h.Comment.Contains("انتقال خودکار"))), Times.Once);
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
                    ActivateAt = DateTime.UtcNow.AddHours(2), // In future
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
        public async Task TriggerImmediateAutomaticTransitionsAsync_ExecutesImmediateTransition_WithoutDelay()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            var factory = CreateInMemoryDbContextFactory(dbName);

            using (var context = await factory.CreateDbContextAsync())
            {
                var s1 = new Status { Id = 102, Name = "New" };
                var s2 = new Status { Id = 202, Name = "Triage" };
                await context.Set<Status>().AddRangeAsync(s1, s2);

                var workflow = new Workflow { Id = 3, Name = "Immediate WF", IsActive = true };
                var status1 = new WorkflowStatus { Id = 12, NodeId = Guid.NewGuid(), StatusId = 102, Status = s1, WorkflowId = 3 };
                var status2 = new WorkflowStatus { Id = 22, NodeId = Guid.NewGuid(), StatusId = 202, Status = s2, WorkflowId = 3 };

                var transition = new Transition
                {
                    Id = 7,
                    Name = "Immediate Triage",
                    FromState = 12,
                    ToState = 22,
                    FromStatus = status1,
                    ToStatus = status2,
                    IsAutomated = 1,
                    IsActive = true,
                    ActivateAt = null, // Immediate
                    WorkflowId = 3,
                    Workflow = workflow
                };

                workflow.WorkflowStatuses.Add(status1);
                workflow.WorkflowStatuses.Add(status2);
                workflow.Transitions.Add(transition);

                var project = new Project { Id = 3, Name = "P3", WorkflowId = 3, Workflow = workflow };
                var ticket = new Ticket
                {
                    Id = 44,
                    Title = "Immediate Ticket",
                    ProjectId = 3,
                    Project = project,
                    WorkflowStatusId = 12,
                    StatusId = 102,
                    Status = s1
                };

                await context.Set<Workflow>().AddAsync(workflow);
                await context.Set<Project>().AddAsync(project);
                await context.Set<Ticket>().AddAsync(ticket);
                await context.SaveChangesAsync();
            }

            var service = new WorkflowAutomationService(factory, _mockTicketRepo.Object, _mockEventBroker.Object, _mockLogger.Object);

            // Act
            await service.TriggerImmediateAutomaticTransitionsAsync(44);

            // Assert
            _mockTicketRepo.Verify(r => r.ApplyTransitionAndSaveHistoryAsync(44, 202, 22, It.IsAny<TicketHistory>()), Times.Once);
            _mockEventBroker.Verify(b => b.PublishTransitionOccurredAsync(44), Times.Once);
        }

        [Fact]
        public async Task TriggerImmediateAutomaticTransitionsAsync_ExecutesCascadingTransitions_Recursively()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            var factory = CreateInMemoryDbContextFactory(dbName);

            using (var context = await factory.CreateDbContextAsync())
            {
                var s1 = new Status { Id = 103, Name = "Step 1" };
                var s2 = new Status { Id = 203, Name = "Step 2" };
                var s3 = new Status { Id = 303, Name = "Step 3" };
                await context.Set<Status>().AddRangeAsync(s1, s2, s3);

                var workflow = new Workflow { Id = 4, Name = "Cascade WF", IsActive = true };
                var status1 = new WorkflowStatus { Id = 13, NodeId = Guid.NewGuid(), StatusId = 103, Status = s1, WorkflowId = 4 };
                var status2 = new WorkflowStatus { Id = 23, NodeId = Guid.NewGuid(), StatusId = 203, Status = s2, WorkflowId = 4 };
                var status3 = new WorkflowStatus { Id = 33, NodeId = Guid.NewGuid(), StatusId = 303, Status = s3, WorkflowId = 4 };

                var tr1 = new Transition
                {
                    Id = 8,
                    Name = "1 to 2",
                    FromState = 13,
                    ToState = 23,
                    FromStatus = status1,
                    ToStatus = status2,
                    IsAutomated = 1,
                    IsActive = true,
                    WorkflowId = 4,
                    Workflow = workflow
                };

                var tr2 = new Transition
                {
                    Id = 9,
                    Name = "2 to 3",
                    FromState = 23,
                    ToState = 33,
                    FromStatus = status2,
                    ToStatus = status3,
                    IsAutomated = 1,
                    IsActive = true,
                    WorkflowId = 4,
                    Workflow = workflow
                };

                workflow.WorkflowStatuses.Add(status1);
                workflow.WorkflowStatuses.Add(status2);
                workflow.WorkflowStatuses.Add(status3);
                workflow.Transitions.Add(tr1);
                workflow.Transitions.Add(tr2);

                var project = new Project { Id = 4, Name = "P4", WorkflowId = 4, Workflow = workflow };
                var ticket = new Ticket
                {
                    Id = 45,
                    Title = "Cascade Ticket",
                    ProjectId = 4,
                    Project = project,
                    WorkflowStatusId = 13,
                    StatusId = 103,
                    Status = s1
                };

                await context.Set<Workflow>().AddAsync(workflow);
                await context.Set<Project>().AddAsync(project);
                await context.Set<Ticket>().AddAsync(ticket);
                await context.SaveChangesAsync();
            }

            _mockTicketRepo.Setup(r => r.ApplyTransitionAndSaveHistoryAsync(45, It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<TicketHistory>()))
                .Returns(Task.CompletedTask);

            var service = new WorkflowAutomationService(factory, _mockTicketRepo.Object, _mockEventBroker.Object, _mockLogger.Object);

            // Act
            await service.TriggerImmediateAutomaticTransitionsAsync(45);

            // Assert
            _mockTicketRepo.Verify(r => r.ApplyTransitionAndSaveHistoryAsync(45, 203, 23, It.IsAny<TicketHistory>()), Times.Once);
            _mockTicketRepo.Verify(r => r.ApplyTransitionAndSaveHistoryAsync(45, 303, 33, It.IsAny<TicketHistory>()), Times.Once);
        }

        [Fact]
        public async Task TriggerImmediateAutomaticTransitionsAsync_PreventsInfiniteLoop_WhenCycleExists()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            var factory = CreateInMemoryDbContextFactory(dbName);

            using (var context = await factory.CreateDbContextAsync())
            {
                var s1 = new Status { Id = 104, Name = "State A" };
                var s2 = new Status { Id = 204, Name = "State B" };
                await context.Set<Status>().AddRangeAsync(s1, s2);

                var workflow = new Workflow { Id = 5, Name = "Loop WF", IsActive = true };
                var status1 = new WorkflowStatus { Id = 14, NodeId = Guid.NewGuid(), StatusId = 104, Status = s1, WorkflowId = 5 };
                var status2 = new WorkflowStatus { Id = 24, NodeId = Guid.NewGuid(), StatusId = 204, Status = s2, WorkflowId = 5 };

                var tr1 = new Transition { Id = 10, Name = "A to B", FromState = 14, ToState = 24, FromStatus = status1, ToStatus = status2, IsAutomated = 1, IsActive = true, WorkflowId = 5, Workflow = workflow };
                var tr2 = new Transition { Id = 11, Name = "B to A", FromState = 24, ToState = 14, FromStatus = status2, ToStatus = status1, IsAutomated = 1, IsActive = true, WorkflowId = 5, Workflow = workflow };

                workflow.WorkflowStatuses.Add(status1);
                workflow.WorkflowStatuses.Add(status2);
                workflow.Transitions.Add(tr1);
                workflow.Transitions.Add(tr2);

                var project = new Project { Id = 5, Name = "P5", WorkflowId = 5, Workflow = workflow };
                var ticket = new Ticket
                {
                    Id = 46,
                    Title = "Loop Ticket",
                    ProjectId = 5,
                    Project = project,
                    WorkflowStatusId = 14,
                    StatusId = 104,
                    Status = s1
                };

                await context.Set<Workflow>().AddAsync(workflow);
                await context.Set<Project>().AddAsync(project);
                await context.Set<Ticket>().AddAsync(ticket);
                await context.SaveChangesAsync();
            }

            _mockTicketRepo.Setup(r => r.ApplyTransitionAndSaveHistoryAsync(46, It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<TicketHistory>()))
                .Returns(Task.CompletedTask);


            var service = new WorkflowAutomationService(factory, _mockTicketRepo.Object, _mockEventBroker.Object, _mockLogger.Object);

            // Act - Should complete safely and terminate at maxDepth = 5
            await service.TriggerImmediateAutomaticTransitionsAsync(46, maxDepth: 5);

            // Assert
            _mockTicketRepo.Verify(r => r.ApplyTransitionAndSaveHistoryAsync(46, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<TicketHistory>()), Times.Exactly(5));
        }

        [Fact]
        public async Task TriggerImmediateAutomaticTransitionsAsync_DoesNotExecute_WhenWorkflowOrTransitionIsInactive()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            var factory = CreateInMemoryDbContextFactory(dbName);

            using (var context = await factory.CreateDbContextAsync())
            {
                var s1 = new Status { Id = 105, Name = "Draft" };
                var s2 = new Status { Id = 205, Name = "Published" };
                await context.Set<Status>().AddRangeAsync(s1, s2);

                var workflow = new Workflow { Id = 6, Name = "Inactive WF", IsActive = false }; // Inactive workflow
                var status1 = new WorkflowStatus { Id = 15, NodeId = Guid.NewGuid(), StatusId = 105, Status = s1, WorkflowId = 6 };
                var status2 = new WorkflowStatus { Id = 25, NodeId = Guid.NewGuid(), StatusId = 205, Status = s2, WorkflowId = 6 };

                var transition = new Transition
                {
                    Id = 12,
                    Name = "Inactive Move",
                    FromState = 15,
                    ToState = 25,
                    FromStatus = status1,
                    ToStatus = status2,
                    IsAutomated = 1,
                    IsActive = true,
                    WorkflowId = 6,
                    Workflow = workflow
                };

                workflow.WorkflowStatuses.Add(status1);
                workflow.WorkflowStatuses.Add(status2);
                workflow.Transitions.Add(transition);

                var project = new Project { Id = 6, Name = "P6", WorkflowId = 6, Workflow = workflow };
                var ticket = new Ticket
                {
                    Id = 47,
                    Title = "Inactive WF Ticket",
                    ProjectId = 6,
                    Project = project,
                    WorkflowStatusId = 15,
                    StatusId = 105,
                    Status = s1
                };

                await context.Set<Workflow>().AddAsync(workflow);
                await context.Set<Project>().AddAsync(project);
                await context.Set<Ticket>().AddAsync(ticket);
                await context.SaveChangesAsync();
            }

            var service = new WorkflowAutomationService(factory, _mockTicketRepo.Object, _mockEventBroker.Object, _mockLogger.Object);

            // Act
            await service.TriggerImmediateAutomaticTransitionsAsync(47);

            // Assert
            _mockTicketRepo.Verify(r => r.ApplyTransitionAndSaveHistoryAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<TicketHistory>()), Times.Never);
        }

        #endregion

        #region Deadline & Overdue Scenarios

        [Fact]
        public async Task ProcessDeadlinesAsync_ExecutesAutomaticEscalationTransition_WhenTicketIsOverdue()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            var factory = CreateInMemoryDbContextFactory(dbName);

            using (var context = await factory.CreateDbContextAsync())
            {
                var user = new User { Id = 10, Name = "agent", Email = "agent@example.com" };
                var s1 = new Status { Id = 500, Name = "Pending Response" };
                var s2 = new Status { Id = 600, Name = "Escalated Due to Timeout" };
                await context.Set<User>().AddAsync(user);
                await context.Set<Status>().AddRangeAsync(s1, s2);

                var workflow = new Workflow { Id = 7, Name = "SLA WF", IsActive = true };
                var status1 = new WorkflowStatus { Id = 50, NodeId = Guid.NewGuid(), StatusId = 500, Status = s1, WorkflowId = 7 };
                var status2 = new WorkflowStatus { Id = 60, NodeId = Guid.NewGuid(), StatusId = 600, Status = s2, WorkflowId = 7 };

                var timeoutTransition = new Transition
                {
                    Id = 20,
                    Name = "Auto Escalate on SLA Breach",
                    FromState = 50,
                    ToState = 60,
                    FromStatus = status1,
                    ToStatus = status2,
                    IsAutomated = 1,
                    IsActive = true,
                    WorkflowId = 7,
                    Workflow = workflow
                };

                workflow.WorkflowStatuses.Add(status1);
                workflow.WorkflowStatuses.Add(status2);
                workflow.Transitions.Add(timeoutTransition);

                var project = new Project { Id = 7, Name = "P7", WorkflowId = 7, Workflow = workflow };
                var ticket = new Ticket
                {
                    Id = 88,
                    Title = "Breached Ticket",
                    UserId = 10,
                    User = user,
                    ProjectId = 7,
                    Project = project,
                    WorkflowStatusId = 50,
                    StatusId = 500,
                    Status = s1,
                    DueDate = DateTime.UtcNow.AddMinutes(-10) // Expired 10 min ago
                };

                await context.Set<Workflow>().AddAsync(workflow);
                await context.Set<Project>().AddAsync(project);
                await context.Set<Ticket>().AddAsync(ticket);
                await context.SaveChangesAsync();
            }

            var service = new WorkflowAutomationService(factory, _mockTicketRepo.Object, _mockEventBroker.Object, _mockLogger.Object);

            // Act
            await service.ProcessDeadlinesAsync();

            // Assert
            _mockTicketRepo.Verify(r => r.ApplyTransitionAndSaveHistoryAsync(
                88,
                600,
                60,
                It.Is<TicketHistory>(h => h.Comment.Contains("Deadline Exceeded") || h.Comment.Contains("اتمام مهلت زمانی"))),
                Times.Once);

            _mockEventBroker.Verify(b => b.PublishTransitionOccurredAsync(88), Times.Once);
            _mockEventBroker.Verify(b => b.PublishTicketUpdatedAsync(88), Times.Once);
        }

        [Fact]
        public async Task ProcessDeadlinesAsync_DoesNotTransition_WhenTicketIsNotYetOverdue()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            var factory = CreateInMemoryDbContextFactory(dbName);

            using (var context = await factory.CreateDbContextAsync())
            {
                var s1 = new Status { Id = 501, Name = "Open" };
                var s2 = new Status { Id = 601, Name = "Escalated" };
                await context.Set<Status>().AddRangeAsync(s1, s2);

                var workflow = new Workflow { Id = 8, Name = "Future SLA WF", IsActive = true };
                var status1 = new WorkflowStatus { Id = 51, NodeId = Guid.NewGuid(), StatusId = 501, Status = s1, WorkflowId = 8 };
                var status2 = new WorkflowStatus { Id = 61, NodeId = Guid.NewGuid(), StatusId = 601, Status = s2, WorkflowId = 8 };

                var timeoutTransition = new Transition
                {
                    Id = 21,
                    Name = "Escalate",
                    FromState = 51,
                    ToState = 61,
                    FromStatus = status1,
                    ToStatus = status2,
                    IsAutomated = 1,
                    IsActive = true,
                    WorkflowId = 8,
                    Workflow = workflow
                };

                workflow.WorkflowStatuses.Add(status1);
                workflow.WorkflowStatuses.Add(status2);
                workflow.Transitions.Add(timeoutTransition);

                var project = new Project { Id = 8, Name = "P8", WorkflowId = 8, Workflow = workflow };
                var ticket = new Ticket
                {
                    Id = 89,
                    Title = "Active Ticket",
                    ProjectId = 8,
                    Project = project,
                    WorkflowStatusId = 51,
                    StatusId = 501,
                    Status = s1,
                    DueDate = DateTime.UtcNow.AddHours(2) // Future deadline!
                };

                await context.Set<Workflow>().AddAsync(workflow);
                await context.Set<Project>().AddAsync(project);
                await context.Set<Ticket>().AddAsync(ticket);
                await context.SaveChangesAsync();
            }

            var service = new WorkflowAutomationService(factory, _mockTicketRepo.Object, _mockEventBroker.Object, _mockLogger.Object);

            // Act
            await service.ProcessDeadlinesAsync();

            // Assert
            _mockTicketRepo.Verify(r => r.ApplyTransitionAndSaveHistoryAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<TicketHistory>()), Times.Never);
            _mockEventBroker.Verify(b => b.PublishTicketUpdatedAsync(89), Times.Never);
        }

        [Fact]
        public async Task ProcessDeadlinesAsync_UpdatesDueDate_WhenDestinationTransitionHasDeadlineMinutes()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            var factory = CreateInMemoryDbContextFactory(dbName);

            using (var context = await factory.CreateDbContextAsync())
            {
                var s1 = new Status { Id = 502, Name = "State 1" };
                var s2 = new Status { Id = 602, Name = "State 2 (Has SLA 60m)" };
                await context.Set<Status>().AddRangeAsync(s1, s2);

                var workflow = new Workflow { Id = 9, Name = "Renewal SLA WF", IsActive = true };
                var status1 = new WorkflowStatus { Id = 52, NodeId = Guid.NewGuid(), StatusId = 502, Status = s1, WorkflowId = 9 };
                var status2 = new WorkflowStatus { Id = 62, NodeId = Guid.NewGuid(), StatusId = 602, Status = s2, WorkflowId = 9 };

                var transition = new Transition
                {
                    Id = 22,
                    Name = "Move and Reset SLA",
                    FromState = 52,
                    ToState = 62,
                    FromStatus = status1,
                    ToStatus = status2,
                    IsAutomated = 1,
                    IsActive = true,
                    DeadlineMinutes = 60, // 60 minutes for stage 2
                    WorkflowId = 9,
                    Workflow = workflow
                };

                workflow.WorkflowStatuses.Add(status1);
                workflow.WorkflowStatuses.Add(status2);
                workflow.Transitions.Add(transition);

                var project = new Project { Id = 9, Name = "P9", WorkflowId = 9, Workflow = workflow };
                var ticket = new Ticket
                {
                    Id = 90,
                    Title = "Renewal Ticket",
                    ProjectId = 9,
                    Project = project,
                    WorkflowStatusId = 52,
                    StatusId = 502,
                    Status = s1,
                    DueDate = DateTime.UtcNow.AddMinutes(-5) // Expired
                };

                await context.Set<Workflow>().AddAsync(workflow);
                await context.Set<Project>().AddAsync(project);
                await context.Set<Ticket>().AddAsync(ticket);
                await context.SaveChangesAsync();
            }

            var service = new WorkflowAutomationService(factory, _mockTicketRepo.Object, _mockEventBroker.Object, _mockLogger.Object);

            // Act
            await service.ProcessDeadlinesAsync();

            // Assert
            using (var verifyContext = await factory.CreateDbContextAsync())
            {
                var updatedTicket = await verifyContext.Set<Ticket>().FindAsync(90);
                Assert.NotNull(updatedTicket);
                Assert.NotNull(updatedTicket.DueDate);
                Assert.True(updatedTicket.DueDate.Value > DateTime.UtcNow.AddMinutes(50)); // Reset to ~60 min from now
            }
        }

        [Fact]
        public async Task ProcessDeadlinesAsync_PublishesUpdate_WhenOverdue_EvenWithoutAutomatedTransition()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            var factory = CreateInMemoryDbContextFactory(dbName);

            using (var context = await factory.CreateDbContextAsync())
            {
                var s1 = new Status { Id = 503, Name = "Under Investigation" };
                await context.Set<Status>().AddAsync(s1);

                var workflow = new Workflow { Id = 10, Name = "No Auto Transition WF", IsActive = true };
                var status1 = new WorkflowStatus { Id = 53, NodeId = Guid.NewGuid(), StatusId = 503, Status = s1, WorkflowId = 10 };
                workflow.WorkflowStatuses.Add(status1);

                var project = new Project { Id = 10, Name = "P10", WorkflowId = 10, Workflow = workflow };
                var ticket = new Ticket
                {
                    Id = 91,
                    Title = "Manual Escalation Ticket",
                    ProjectId = 10,
                    Project = project,
                    WorkflowStatusId = 53,
                    StatusId = 503,
                    Status = s1,
                    DueDate = DateTime.UtcNow.AddMinutes(-15) // Overdue
                };

                await context.Set<Workflow>().AddAsync(workflow);
                await context.Set<Project>().AddAsync(project);
                await context.Set<Ticket>().AddAsync(ticket);
                await context.SaveChangesAsync();
            }

            var service = new WorkflowAutomationService(factory, _mockTicketRepo.Object, _mockEventBroker.Object, _mockLogger.Object);

            // Act
            await service.ProcessDeadlinesAsync();

            // Assert
            _mockTicketRepo.Verify(r => r.ApplyTransitionAndSaveHistoryAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<TicketHistory>()), Times.Never);
            _mockEventBroker.Verify(b => b.PublishTicketUpdatedAsync(91), Times.Once); // Real-time notification sent
        }

        [Fact]
        public async Task ProcessDeadlinesAsync_SendsNotification_WhenNotificationServiceIsProvided()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            var factory = CreateInMemoryDbContextFactory(dbName);
            var mockNotifService = new Mock<INotificationService>();
            mockNotifService.Setup(n => n.CreateNotificationAsync(It.IsAny<TicketHub.Application.DTOs.CreateNotificationDto>()))
                .ReturnsAsync(new TicketHub.Application.DTOs.NotificationDto());

            using (var context = await factory.CreateDbContextAsync())
            {
                var user = new User { Id = 55, Name = "user55", Email = "u55@example.com" };
                var s1 = new Status { Id = 700, Name = "Open" };
                var s2 = new Status { Id = 800, Name = "Closed by SLA" };
                await context.Set<User>().AddAsync(user);
                await context.Set<Status>().AddRangeAsync(s1, s2);

                var workflow = new Workflow { Id = 11, Name = "Notif SLA WF", IsActive = true };
                var status1 = new WorkflowStatus { Id = 70, NodeId = Guid.NewGuid(), StatusId = 700, Status = s1, WorkflowId = 11 };
                var status2 = new WorkflowStatus { Id = 80, NodeId = Guid.NewGuid(), StatusId = 800, Status = s2, WorkflowId = 11 };

                var transition = new Transition
                {
                    Id = 30,
                    Name = "Auto Close",
                    FromState = 70,
                    ToState = 80,
                    FromStatus = status1,
                    ToStatus = status2,
                    IsAutomated = 1,
                    IsActive = true,
                    WorkflowId = 11,
                    Workflow = workflow
                };

                workflow.WorkflowStatuses.Add(status1);
                workflow.WorkflowStatuses.Add(status2);
                workflow.Transitions.Add(transition);

                var project = new Project { Id = 11, Name = "P11", WorkflowId = 11, Workflow = workflow };
                var ticket = new Ticket
                {
                    Id = 95,
                    Title = "Notif Ticket",
                    UserId = 55,
                    User = user,
                    ProjectId = 11,
                    Project = project,
                    WorkflowStatusId = 70,
                    StatusId = 700,
                    Status = s1,
                    DueDate = DateTime.UtcNow.AddMinutes(-5) // Overdue
                };

                await context.Set<Workflow>().AddAsync(workflow);
                await context.Set<Project>().AddAsync(project);
                await context.Set<Ticket>().AddAsync(ticket);
                await context.SaveChangesAsync();
            }

            var service = new WorkflowAutomationService(factory, _mockTicketRepo.Object, _mockEventBroker.Object, _mockLogger.Object, mockNotifService.Object);

            // Act
            await service.ProcessDeadlinesAsync();

            // Assert
            mockNotifService.Verify(n => n.CreateNotificationAsync(It.Is<TicketHub.Application.DTOs.CreateNotificationDto>(
                dto => dto.UserId == 55 && dto.ReferenceId == 95 && dto.Type == TicketHub.Core.Enums.NotificationType.DeadlineBreached)), Times.Once);
        }

        #endregion
    }
}

