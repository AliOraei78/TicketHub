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
    public class StatusServiceTests
    {
        private readonly Mock<IRepository<Status>> _mockRepo;
        private readonly Mock<ILogger<StatusService>> _mockLogger;
        private readonly Mock<IValidator<StatusDto>> _mockValidator;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly Mock<IPermissionService> _mockPermissionService;
        private readonly StatusService _service;

        public StatusServiceTests()
        {
            _mockRepo = new Mock<IRepository<Status>>();
            _mockLogger = new Mock<ILogger<StatusService>>();
            _mockValidator = new Mock<IValidator<StatusDto>>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _mockPermissionService = new Mock<IPermissionService>();

            // Default permission setup: Access granted
            _mockPermissionService.Setup(p => p.HasAccessAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>(), It.IsAny<PermissionType>()))
                .ReturnsAsync(true);

            // Default validator setup: Valid
            _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<StatusDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _service = new StatusService(
                _mockRepo.Object,
                _mockLogger.Object,
                _mockValidator.Object,
                _mockHttpContextAccessor.Object,
                _mockPermissionService.Object
            );
        }

        [Fact]
        public async Task GetAllAsync_ReturnsMappedStatusDtos()
        {
            var statuses = new List<Status>
            {
                new Status { Id = 1, Name = "Open", ColorCode = "#FF0000", IsActive = true },
                new Status { Id = 2, Name = "Closed", ColorCode = "#00FF00", IsActive = false }
            };

            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(statuses);

            var result = await _service.GetAllAsync();

            result.Should().HaveCount(2);
            result.First().Name.Should().Be("Open");
            result.Last().IsActive.Should().BeFalse();
        }

        [Fact]
        public async Task AddAsync_WithValidDto_AddsToRepository()
        {
            var dto = new StatusDto { Name = "In Progress", ColorCode = "#3B82F6", IsActive = true };

            await _service.AddAsync(dto);

            _mockRepo.Verify(r => r.AddAsync(It.Is<Status>(s => s.Name == "In Progress" && s.ColorCode == "#3B82F6")), Times.Once);
        }

        [Fact]
        public async Task AddAsync_ValidationFails_ThrowsValidationException()
        {
            var dto = new StatusDto { Name = "" };
            var failures = new List<ValidationFailure> { new ValidationFailure("Name", "نام وضعیت الزامی است") };
            _mockValidator.Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(failures));

            Func<Task> act = () => _service.AddAsync(dto);

            await act.Should().ThrowAsync<ValidationException>();
            _mockRepo.Verify(r => r.AddAsync(It.IsAny<Status>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_StatusExists_UpdatesRepository()
        {
            var existing = new Status { Id = 1, Name = "OldStatus", ColorCode = "#000000" };
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);

            var dto = new StatusDto { Id = 1, Name = "NewStatus", ColorCode = "#10B981" };

            await _service.UpdateAsync(dto);

            _mockRepo.Verify(r => r.UpdateAsync(It.Is<Status>(s => s.Id == 1 && s.Name == "NewStatus")), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_StatusNotFound_ThrowsNotFoundException()
        {
            _mockRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Status?)null);

            var dto = new StatusDto { Id = 999, Name = "Missing" };

            Func<Task> act = () => _service.UpdateAsync(dto);

            await act.Should().ThrowAsync<NotFoundException>();
            _mockRepo.Verify(r => r.UpdateAsync(It.IsAny<Status>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_StatusExists_DeletesFromRepository()
        {
            var existing = new Status { Id = 5, Name = "ToDelete" };
            _mockRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(existing);

            await _service.DeleteAsync(5);

            _mockRepo.Verify(r => r.DeleteAsync(5), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_StatusNotFound_ThrowsNotFoundException()
        {
            _mockRepo.Setup(r => r.GetByIdAsync(404)).ReturnsAsync((Status?)null);

            Func<Task> act = () => _service.DeleteAsync(404);

            await act.Should().ThrowAsync<NotFoundException>();
            _mockRepo.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task DeleteRangeAsync_MultipleExistingStatuses_DeletesRange()
        {
            var statuses = new List<Status>
            {
                new Status { Id = 10, Name = "S10" },
                new Status { Id = 20, Name = "S20" },
                new Status { Id = 30, Name = "S30" }
            };
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(statuses);

            await _service.DeleteRangeAsync(new[] { 10, 20 });

            _mockRepo.Verify(r => r.DeleteRangeAsync(It.Is<IEnumerable<Status>>(s => s.Count() == 2)), Times.Once);
        }

        [Fact]
        public async Task DeleteRangeAsync_NoMatchingStatuses_ThrowsNotFoundException()
        {
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Status>());

            Func<Task> act = () => _service.DeleteRangeAsync(new[] { 99 });

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task UpdateStatesStatusAsync_UpdatesStatusForMultiple()
        {
            var statuses = new List<Status>
            {
                new Status { Id = 1, Name = "S1", IsActive = false },
                new Status { Id = 2, Name = "S2", IsActive = false }
            };
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(statuses);

            await _service.UpdateStatesStatusAsync(new[] { 1, 2 }, true);

            _mockRepo.Verify(r => r.UpdateAsync(It.Is<Status>(s => s.IsActive)), Times.Exactly(2));
        }

        [Fact]
        public async Task UpdateStatesStatusAsync_NoMatching_ThrowsNotFoundException()
        {
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Status>());

            Func<Task> act = () => _service.UpdateStatesStatusAsync(new[] { 99 }, true);

            await act.Should().ThrowAsync<NotFoundException>();
        }
    }
}
