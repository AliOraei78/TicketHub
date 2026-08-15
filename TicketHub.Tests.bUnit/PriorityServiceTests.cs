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
    public class PriorityServiceTests
    {
        private readonly Mock<IRepository<Priority>> _mockRepo;
        private readonly Mock<ILogger<PriorityService>> _mockLogger;
        private readonly Mock<IValidator<PriorityDto>> _mockValidator;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly Mock<IPermissionService> _mockPermissionService;
        private readonly PriorityService _service;

        public PriorityServiceTests()
        {
            _mockRepo = new Mock<IRepository<Priority>>();
            _mockLogger = new Mock<ILogger<PriorityService>>();
            _mockValidator = new Mock<IValidator<PriorityDto>>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _mockPermissionService = new Mock<IPermissionService>();

            // Default permission setup: Access granted
            _mockPermissionService.Setup(p => p.HasAccessAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>(), It.IsAny<PermissionType>()))
                .ReturnsAsync(true);

            // Default validator setup: Valid
            _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<PriorityDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _service = new PriorityService(
                _mockRepo.Object,
                _mockLogger.Object,
                _mockValidator.Object,
                _mockHttpContextAccessor.Object,
                _mockPermissionService.Object
            );
        }

        [Fact]
        public async Task GetAllAsync_ReturnsMappedPriorityDtos()
        {
            var priorities = new List<Priority>
            {
                new Priority { Id = 1, Name = "High", Level = 10, ColorCode = "#FF0000", IsActive = true },
                new Priority { Id = 2, Name = "Low", Level = 1, ColorCode = "#00FF00", IsActive = false }
            };

            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(priorities);

            var result = await _service.GetAllAsync();

            result.Should().HaveCount(2);
            result.First().Name.Should().Be("High");
            result.Last().IsActive.Should().BeFalse();
        }

        [Fact]
        public async Task AddAsync_WithValidDto_AddsToRepository()
        {
            var dto = new PriorityDto { Name = "Urgent", Level = 20, ColorCode = "#EF4444", IsActive = true };

            await _service.AddAsync(dto);

            _mockRepo.Verify(r => r.AddAsync(It.Is<Priority>(p => p.Name == "Urgent" && p.Level == 20)), Times.Once);
        }

        [Fact]
        public async Task AddAsync_ValidationFails_ThrowsValidationException()
        {
            var dto = new PriorityDto { Name = "" };
            var failures = new List<ValidationFailure> { new ValidationFailure("Name", "نام اولویت الزامی است") };
            _mockValidator.Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(failures));

            Func<Task> act = () => _service.AddAsync(dto);

            await act.Should().ThrowAsync<ValidationException>();
            _mockRepo.Verify(r => r.AddAsync(It.IsAny<Priority>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_PriorityExists_UpdatesRepository()
        {
            var existing = new Priority { Id = 1, Name = "Old", Level = 5 };
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);

            var dto = new PriorityDto { Id = 1, Name = "New", Level = 10 };

            await _service.UpdateAsync(dto);

            _mockRepo.Verify(r => r.UpdateAsync(It.Is<Priority>(p => p.Id == 1 && p.Name == "New" && p.Level == 10)), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_PriorityNotFound_ThrowsNotFoundException()
        {
            _mockRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Priority?)null);

            var dto = new PriorityDto { Id = 999, Name = "Missing" };

            Func<Task> act = () => _service.UpdateAsync(dto);

            await act.Should().ThrowAsync<NotFoundException>();
            _mockRepo.Verify(r => r.UpdateAsync(It.IsAny<Priority>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_PriorityExists_DeletesFromRepository()
        {
            var existing = new Priority { Id = 3, Name = "ToDelete" };
            _mockRepo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(existing);

            await _service.DeleteAsync(3);

            _mockRepo.Verify(r => r.DeleteAsync(3), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_PriorityNotFound_ThrowsNotFoundException()
        {
            _mockRepo.Setup(r => r.GetByIdAsync(404)).ReturnsAsync((Priority?)null);

            Func<Task> act = () => _service.DeleteAsync(404);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task DeleteRangeAsync_DeletesMultiplePriorities()
        {
            var priorities = new List<Priority>
            {
                new Priority { Id = 1, Name = "P1" },
                new Priority { Id = 2, Name = "P2" }
            };
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(priorities);

            await _service.DeleteRangeAsync(new[] { 1, 2 });

            _mockRepo.Verify(r => r.DeleteRangeAsync(It.Is<IEnumerable<Priority>>(p => p.Count() == 2)), Times.Once);
        }

        [Fact]
        public async Task DeleteRangeAsync_NoMatching_ThrowsNotFoundException()
        {
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Priority>());

            Func<Task> act = () => _service.DeleteRangeAsync(new[] { 99 });

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task UpdatePrioritiesStatusAsync_UpdatesStatusForMultiple()
        {
            var priorities = new List<Priority>
            {
                new Priority { Id = 1, Name = "P1", IsActive = false },
                new Priority { Id = 2, Name = "P2", IsActive = false }
            };
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(priorities);

            await _service.UpdatePrioritiesStatusAsync(new[] { 1, 2 }, true);

            _mockRepo.Verify(r => r.UpdateAsync(It.Is<Priority>(p => p.IsActive)), Times.Exactly(2));
        }

        [Fact]
        public async Task UpdatePrioritiesStatusAsync_NoMatching_ThrowsNotFoundException()
        {
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Priority>());

            Func<Task> act = () => _service.UpdatePrioritiesStatusAsync(new[] { 99 }, true);

            await act.Should().ThrowAsync<NotFoundException>();
        }
    }
}
