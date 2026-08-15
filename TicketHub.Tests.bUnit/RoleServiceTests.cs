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
using TicketHub.Application.Validators;
using TicketHub.Core.Common.Exceptions;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;
using Xunit;
using ValidationException = TicketHub.Core.Common.Exceptions.ValidationException;

namespace TicketHub.Tests.bUnit
{
    public class RoleServiceTests
    {
        private readonly Mock<IRepository<Role>> _mockRepo;
        private readonly Mock<ILogger<RoleService>> _mockLogger;
        private readonly Mock<IValidator<RoleDto>> _mockValidator;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly Mock<IPermissionService> _mockPermissionService;
        private readonly RoleService _service;

        public RoleServiceTests()
        {
            _mockRepo = new Mock<IRepository<Role>>();
            _mockLogger = new Mock<ILogger<RoleService>>();
            _mockValidator = new Mock<IValidator<RoleDto>>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _mockPermissionService = new Mock<IPermissionService>();

            // Default permission setup: Access granted
            _mockPermissionService.Setup(p => p.HasAccessAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>(), It.IsAny<PermissionType>()))
                .ReturnsAsync(true);

            // Default validator setup: Valid
            _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<RoleDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _service = new RoleService(
                _mockRepo.Object,
                _mockLogger.Object,
                _mockValidator.Object,
                _mockHttpContextAccessor.Object,
                _mockPermissionService.Object
            );
        }

        [Fact]
        public async Task GetAllRolesAsync_ReturnsMappedRoleDtos()
        {
            var roles = new List<Role>
            {
                new Role { Id = 1, Name = "Admin", IsActive = true },
                new Role { Id = 2, Name = "Support", IsActive = false }
            };

            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(roles);

            var result = await _service.GetAllRolesAsync();

            result.Should().HaveCount(2);
            result.First().Name.Should().Be("Admin");
            result.Last().IsActive.Should().BeFalse();
        }

        [Fact]
        public async Task CreateRoleAsync_WithValidDto_AddsToRepository()
        {
            var roleDto = new RoleDto { Name = "TechLead", IsActive = true };

            await _service.CreateRoleAsync(roleDto);

            _mockRepo.Verify(r => r.AddAsync(It.Is<Role>(role => role.Name == "TechLead" && role.IsActive)), Times.Once);
        }

        [Fact]
        public async Task CreateRoleAsync_ValidationFails_ThrowsValidationException()
        {
            var roleDto = new RoleDto { Name = "" };
            var failures = new List<ValidationFailure> { new ValidationFailure("Name", "نام نقش الزامی است") };
            _mockValidator.Setup(v => v.ValidateAsync(roleDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(failures));

            Func<Task> act = () => _service.CreateRoleAsync(roleDto);

            await act.Should().ThrowAsync<ValidationException>();
            _mockRepo.Verify(r => r.AddAsync(It.IsAny<Role>()), Times.Never);
        }

        [Fact]
        public async Task UpdateRoleAsync_RoleExists_UpdatesRepository()
        {
            var existingRole = new Role { Id = 1, Name = "OldName", IsActive = false };
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingRole);

            var roleDto = new RoleDto { Id = 1, Name = "NewName", IsActive = true };

            await _service.UpdateRoleAsync(1, roleDto);

            _mockRepo.Verify(r => r.UpdateAsync(It.Is<Role>(role => role.Id == 1 && role.Name == "NewName" && role.IsActive)), Times.Once);
        }

        [Fact]
        public async Task UpdateRoleAsync_RoleNotFound_ThrowsNotFoundException()
        {
            _mockRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Role?)null);

            var roleDto = new RoleDto { Id = 999, Name = "Missing" };

            Func<Task> act = () => _service.UpdateRoleAsync(999, roleDto);

            await act.Should().ThrowAsync<NotFoundException>();
            _mockRepo.Verify(r => r.UpdateAsync(It.IsAny<Role>()), Times.Never);
        }

        [Fact]
        public async Task DeleteRoleAsync_RoleExists_DeletesFromRepository()
        {
            var existingRole = new Role { Id = 3, Name = "ToDelete" };
            _mockRepo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(existingRole);

            await _service.DeleteRoleAsync(3);

            _mockRepo.Verify(r => r.DeleteAsync(3), Times.Once);
        }

        [Fact]
        public async Task DeleteRoleAsync_RoleNotFound_ThrowsNotFoundException()
        {
            _mockRepo.Setup(r => r.GetByIdAsync(404)).ReturnsAsync((Role?)null);

            Func<Task> act = () => _service.DeleteRoleAsync(404);

            await act.Should().ThrowAsync<NotFoundException>();
            _mockRepo.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task DeleteRolesAsync_MultipleExistingRoles_DeletesRange()
        {
            var roles = new List<Role>
            {
                new Role { Id = 10, Name = "Role 10" },
                new Role { Id = 20, Name = "Role 20" },
                new Role { Id = 30, Name = "Role 30" }
            };
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(roles);

            await _service.DeleteRolesAsync(new[] { 10, 20 });

            _mockRepo.Verify(r => r.DeleteRangeAsync(It.Is<IEnumerable<Role>>(r => r.Count() == 2)), Times.Once);
        }

        [Fact]
        public async Task DeleteRolesAsync_NoMatchingRoles_ThrowsNotFoundException()
        {
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Role>());

            Func<Task> act = () => _service.DeleteRolesAsync(new[] { 99 });

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task UpdateRolesStatusAsync_UpdatesStatusForMultipleRoles()
        {
            var roles = new List<Role>
            {
                new Role { Id = 1, Name = "R1", IsActive = false },
                new Role { Id = 2, Name = "R2", IsActive = false }
            };
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(roles);

            await _service.UpdateRolesStatusAsync(new[] { 1, 2 }, true);

            _mockRepo.Verify(r => r.UpdateAsync(It.Is<Role>(r => r.IsActive)), Times.Exactly(2));
        }

        [Fact]
        public async Task UpdateRolesStatusAsync_NoMatchingRoles_ThrowsNotFoundException()
        {
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Role>());

            Func<Task> act = () => _service.UpdateRolesStatusAsync(new[] { 99 }, true);

            await act.Should().ThrowAsync<NotFoundException>();
        }
    }
}
