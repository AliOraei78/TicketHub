using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using TicketHub.Application.DTOs;
using TicketHub.Application.Enums;
using TicketHub.Application.Services;
using TicketHub.Core.Common.Exceptions;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;
using Xunit;
using ValidationException = TicketHub.Core.Common.Exceptions.ValidationException;

namespace TicketHub.Tests.bUnit
{
    public class PermissionServiceTests
    {
        private readonly Mock<IRepository<Permission>> _mockPermissionRepo;
        private readonly Mock<IRepository<RolePermission>> _mockRolePermissionRepo;
        private readonly IMemoryCache _memoryCache;
        private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
        private readonly Mock<ILogger<PermissionService>> _mockLogger;
        private readonly Mock<IValidator<PermissionDto>> _mockValidator;
        private readonly PermissionService _service;

        public PermissionServiceTests()
        {
            _mockPermissionRepo = new Mock<IRepository<Permission>>();
            _mockRolePermissionRepo = new Mock<IRepository<RolePermission>>();
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            _mockScopeFactory = new Mock<IServiceScopeFactory>();
            _mockLogger = new Mock<ILogger<PermissionService>>();
            _mockValidator = new Mock<IValidator<PermissionDto>>();

            // Default validator setup: Valid
            _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<PermissionDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _service = new PermissionService(
                _mockPermissionRepo.Object,
                _mockRolePermissionRepo.Object,
                _memoryCache,
                _mockScopeFactory.Object,
                _mockLogger.Object,
                _mockValidator.Object
            );
        }

        [Fact]
        public async Task GetAllAsync_ReturnsMappedPermissionDtos()
        {
            var permissions = new List<Permission>
            {
                new Permission { Id = 1, Title = "Perm1", ResourceKey = "res1", Type = PermissionType.Full, IsActive = true },
                new Permission { Id = 2, Title = "Perm2", ResourceKey = "res2", Type = PermissionType.Menu, IsActive = false }
            };

            _mockPermissionRepo.Setup(r => r.GetAllWithIncludesAsync(It.IsAny<Expression<Func<Permission, object?>>[]>()))
                .ReturnsAsync(permissions);

            var result = await _service.GetAllAsync();

            result.Should().HaveCount(2);
            result.First().Title.Should().Be("Perm1");
            result.Last().IsActive.Should().BeFalse();
        }

        [Fact]
        public async Task GetByIdAsync_PermissionExists_ReturnsPermissionDto()
        {
            var permissions = new List<Permission>
            {
                new Permission { Id = 5, Title = "Perm5", ResourceKey = "res5", Type = PermissionType.Full }
            };

            _mockPermissionRepo.Setup(r => r.GetAllWithIncludesAsync(It.IsAny<Expression<Func<Permission, object?>>[]>()))
                .ReturnsAsync(permissions);

            var result = await _service.GetByIdAsync(5);

            result.Should().NotBeNull();
            result!.Title.Should().Be("Perm5");
        }

        [Fact]
        public async Task GetByIdAsync_NotFound_ThrowsNotFoundException()
        {
            _mockPermissionRepo.Setup(r => r.GetAllWithIncludesAsync(It.IsAny<Expression<Func<Permission, object?>>[]>()))
                .ReturnsAsync(new List<Permission>());

            Func<Task> act = () => _service.GetByIdAsync(999);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task CreateAsync_WithValidDto_AddsToRepository()
        {
            var dto = new PermissionDto
            {
                Title = "NewPerm",
                ResourceKey = "res.new",
                Type = PermissionType.Full,
                RoleIds = new List<int> { 1, 2 }
            };

            var result = await _service.CreateAsync(dto);

            _mockPermissionRepo.Verify(r => r.AddAsync(It.Is<Permission>(p => p.Title == "NewPerm" && p.RolePermissions.Count == 2)), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ValidationFails_ThrowsValidationException()
        {
            var dto = new PermissionDto { Title = "" };
            var failures = new List<ValidationFailure> { new ValidationFailure("Title", "عنوان الزامی است") };
            _mockValidator.Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(failures));

            Func<Task> act = () => _service.CreateAsync(dto);

            await act.Should().ThrowAsync<ValidationException>();
            _mockPermissionRepo.Verify(r => r.AddAsync(It.IsAny<Permission>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_PermissionExists_UpdatesRepository()
        {
            var existing = new Permission { Id = 1, Title = "Old", ResourceKey = "old.key" };
            _mockPermissionRepo.Setup(r => r.GetAllWithIncludesAsync(It.IsAny<Expression<Func<Permission, object?>>[]>()))
                .ReturnsAsync(new List<Permission> { existing });
            _mockRolePermissionRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<RolePermission>());

            var dto = new PermissionDto { Id = 1, Title = "Updated", ResourceKey = "updated.key", RoleIds = new List<int> { 5 } };

            await _service.UpdateAsync(dto);

            _mockPermissionRepo.Verify(r => r.UpdateAsync(It.Is<Permission>(p => p.Id == 1 && p.Title == "Updated")), Times.Once);
            _mockRolePermissionRepo.Verify(r => r.AddAsync(It.Is<RolePermission>(rp => rp.RoleId == 5)), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_PermissionNotFound_ThrowsNotFoundException()
        {
            _mockPermissionRepo.Setup(r => r.GetAllWithIncludesAsync(It.IsAny<Expression<Func<Permission, object?>>[]>()))
                .ReturnsAsync(new List<Permission>());

            var dto = new PermissionDto { Id = 404, Title = "Missing" };

            Func<Task> act = () => _service.UpdateAsync(dto);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task DeleteAsync_PermissionExists_DeletesFromRepository()
        {
            var existing = new Permission { Id = 10, Title = "ToDelete" };
            _mockPermissionRepo.Setup(r => r.GetAllWithIncludesAsync(It.IsAny<Expression<Func<Permission, object?>>[]>()))
                .ReturnsAsync(new List<Permission> { existing });
            _mockRolePermissionRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<RolePermission>());

            await _service.DeleteAsync(10);

            _mockPermissionRepo.Verify(r => r.DeleteAsync(10), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_PermissionNotFound_ThrowsNotFoundException()
        {
            _mockPermissionRepo.Setup(r => r.GetAllWithIncludesAsync(It.IsAny<Expression<Func<Permission, object?>>[]>()))
                .ReturnsAsync(new List<Permission>());

            Func<Task> act = () => _service.DeleteAsync(404);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task DeleteRangeAsync_DeletesMultiplePermissions()
        {
            var permissions = new List<Permission>
            {
                new Permission { Id = 1, Title = "P1" },
                new Permission { Id = 2, Title = "P2" }
            };
            _mockPermissionRepo.Setup(r => r.GetAllWithIncludesAsync(It.IsAny<Expression<Func<Permission, object?>>[]>()))
                .ReturnsAsync(permissions);
            _mockRolePermissionRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<RolePermission>());

            await _service.DeleteRangeAsync(new[] { 1, 2 });

            _mockPermissionRepo.Verify(r => r.DeleteRangeAsync(It.Is<IEnumerable<Permission>>(p => p.Count() == 2)), Times.Once);
        }

        [Fact]
        public async Task UpdateStatusAsync_UpdatesStatusForMultiple()
        {
            var permissions = new List<Permission>
            {
                new Permission { Id = 1, Title = "P1", IsActive = false },
                new Permission { Id = 2, Title = "P2", IsActive = false }
            };
            _mockPermissionRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(permissions);

            await _service.UpdateStatusAsync(new[] { 1, 2 }, true);

            _mockPermissionRepo.Verify(r => r.UpdateAsync(It.Is<Permission>(p => p.IsActive)), Times.Exactly(2));
        }
    }
}
