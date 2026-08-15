using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
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
    public class WorkflowServiceTests
    {
        private readonly Mock<IWorkflowRepository> _mockRepo;
        private readonly Mock<ILogger<WorkflowService>> _mockLogger;
        private readonly Mock<IValidator<WorkflowDto>> _mockValidator;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly Mock<IPermissionService> _mockPermissionService;

        private readonly WorkflowService _service;

        public WorkflowServiceTests()
        {
            _mockRepo = new Mock<IWorkflowRepository>();
            _mockLogger = new Mock<ILogger<WorkflowService>>();
            _mockValidator = new Mock<IValidator<WorkflowDto>>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _mockPermissionService = new Mock<IPermissionService>();

            _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<WorkflowDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _mockPermissionService.Setup(p => p.HasAccessAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>(), It.IsAny<PermissionType>()))
                .ReturnsAsync(true);

            _service = new WorkflowService(
                _mockRepo.Object,
                _mockLogger.Object,
                _mockValidator.Object,
                _mockHttpContextAccessor.Object,
                _mockPermissionService.Object
            );
        }

        [Fact]
        public async Task GetAllAsync_ReturnsMappedWorkflows()
        {
            var workflows = new List<Workflow>
            {
                new Workflow { Id = 1, Name = "WF 1" },
                new Workflow { Id = 2, Name = "WF 2" }
            };
            _mockRepo.Setup(r => r.GetAllWithDetailsAsync()).ReturnsAsync(workflows);

            var result = await _service.GetAllAsync();

            result.Should().NotBeNull();
            result.Count.Should().Be(2);
            result.First().Name.Should().Be("WF 1");
        }

        [Fact]
        public async Task GetByIdWithDetailsAsync_NonExistent_ThrowsNotFoundException()
        {
            _mockRepo.Setup(r => r.GetWorkflowWithDetailsAsync(99)).ReturnsAsync((Workflow?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdWithDetailsAsync(99));
        }

        [Fact]
        public async Task GetByIdWithDetailsAsync_Existing_ReturnsWorkflowDto()
        {
            var wf = new Workflow { Id = 5, Name = "Bug Flow" };
            _mockRepo.Setup(r => r.GetWorkflowWithDetailsAsync(5)).ReturnsAsync(wf);

            var result = await _service.GetByIdWithDetailsAsync(5);

            result.Should().NotBeNull();
            result!.Id.Should().Be(5);
            result.Name.Should().Be("Bug Flow");
        }

        [Fact]
        public async Task CreateAsync_ValidationFails_ThrowsValidationException()
        {
            var failures = new List<ValidationFailure>
            {
                new ValidationFailure("Name", "نام جریان کاری الزامی است.")
            };
            _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<WorkflowDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(failures));

            var dto = new WorkflowDto { Name = "" };

            await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(dto));
            _mockRepo.Verify(r => r.AddAsync(It.IsAny<Workflow>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_Valid_MapsTransitionsAndAddsWorkflow()
        {
            var nodeA = Guid.NewGuid();
            var nodeB = Guid.NewGuid();

            var dto = new WorkflowDto
            {
                Name = "جریان اصلی",
                WorkflowStatuses = new List<WorkflowStatusDto>
                {
                    new WorkflowStatusDto { NodeId = nodeA, StatusId = 1, IsInitial = true },
                    new WorkflowStatusDto { NodeId = nodeB, StatusId = 2 }
                },
                Transitions = new List<TransitionDto>
                {
                    new TransitionDto { FromNodeId = nodeA, ToNodeId = nodeB, Name = "تایید" }
                }
            };

            var created = await _service.CreateAsync(dto);

            _mockRepo.Verify(r => r.AddAsync(It.Is<Workflow>(w => w.Name == "جریان اصلی")), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_Valid_CallsRepositoryUpdate()
        {
            var dto = new WorkflowDto { Id = 3, Name = "نام جدید" };

            await _service.UpdateAsync(dto);

            _mockRepo.Verify(r => r.UpdateAsync(It.Is<Workflow>(w => w.Id == 3 && w.Name == "نام جدید")), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_NonExistent_ThrowsNotFoundException()
        {
            _mockRepo.Setup(r => r.GetWorkflowWithDetailsAsync(44)).ReturnsAsync((Workflow?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync(44));
            _mockRepo.Verify(r => r.DeleteAsync(44), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_Existing_DeletesWorkflow()
        {
            var wf = new Workflow { Id = 10, Name = "جریان حذف" };
            _mockRepo.Setup(r => r.GetWorkflowWithDetailsAsync(10)).ReturnsAsync(wf);

            await _service.DeleteAsync(10);

            _mockRepo.Verify(r => r.DeleteAsync(10), Times.Once);
        }

        [Fact]
        public async Task DeleteRangeAsync_EmptyList_ThrowsNotFoundException()
        {
            await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteRangeAsync(new List<int>()));
        }

        [Fact]
        public async Task DeleteRangeAsync_ValidIds_CallsRepositoryDeleteRange()
        {
            var ids = new List<int> { 1, 2, 3 };

            await _service.DeleteRangeAsync(ids);

            _mockRepo.Verify(r => r.DeleteRangeAsync(It.Is<List<int>>(l => l.Count == 3)), Times.Once);
        }
    }
}
