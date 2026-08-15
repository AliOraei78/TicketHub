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
    public class UserServiceTests
    {
        private readonly Mock<IUserRepository> _mockUserRepo;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<IRepository<Role>> _mockRoleRepo;
        private readonly Mock<ILogger<UserService>> _mockLogger;
        private readonly Mock<IValidator<UserDto>> _mockValidator;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly Mock<IPermissionService> _mockPermissionService;

        private readonly UserService _service;

        public UserServiceTests()
        {
            _mockUserRepo = new Mock<IUserRepository>();
            _mockEmailService = new Mock<IEmailService>();
            _mockRoleRepo = new Mock<IRepository<Role>>();
            _mockLogger = new Mock<ILogger<UserService>>();
            _mockValidator = new Mock<IValidator<UserDto>>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _mockPermissionService = new Mock<IPermissionService>();

            _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<UserDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _mockPermissionService.Setup(p => p.HasAccessAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>(), It.IsAny<PermissionType>()))
                .ReturnsAsync(true);

            _service = new UserService(
                _mockUserRepo.Object,
                _mockEmailService.Object,
                _mockRoleRepo.Object,
                _mockLogger.Object,
                _mockValidator.Object,
                _mockHttpContextAccessor.Object,
                _mockPermissionService.Object
            );
        }

        [Fact]
        public async Task GetAllAsync_ReturnsMappedUsers()
        {
            var users = new List<User>
            {
                new User { Id = 1, Name = "Ali", Email = "ali@test.com" },
                new User { Id = 2, Name = "Reza", Email = "reza@test.com" }
            };
            _mockUserRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(users);

            var result = await _service.GetAllAsync();

            result.Should().NotBeNull();
            result.Count.Should().Be(2);
            result.First().Name.Should().Be("Ali");
        }

        [Fact]
        public async Task GetByIdAsync_NonExistentUser_ThrowsNotFoundException()
        {
            _mockUserRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(99));
        }

        [Fact]
        public async Task GetByIdAsync_ExistingUser_ReturnsUserDto()
        {
            var user = new User { Id = 5, Name = "Sarah", Email = "sarah@test.com" };
            _mockUserRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(user);

            var result = await _service.GetByIdAsync(5);

            result.Should().NotBeNull();
            result!.Id.Should().Be(5);
            result.Name.Should().Be("Sarah");
        }

        [Fact]
        public async Task CreateAsync_WhenValidationFails_ThrowsValidationException()
        {
            var failures = new List<ValidationFailure>
            {
                new ValidationFailure("Email", "ایمیل نامعتبر است.")
            };
            _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<UserDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(failures));

            var dto = new UserDto { Name = "Ali", Email = "invalid" };

            await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(dto, "Strong@123", new List<int>()));
            _mockUserRepo.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_WhenPasswordWeak_ThrowsValidationException()
        {
            var dto = new UserDto { Name = "Ali", Email = "ali@test.com" };

            // Weak password
            await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(dto, "123", new List<int>()));
            _mockUserRepo.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_ValidUser_AddsUserAndUpdatesRoles()
        {
            var dto = new UserDto
            {
                Name = "کاربر تستی جدید",
                Email = "new@test.com",
                PhoneNumber = "09121234567"
            };

            await _service.CreateAsync(dto, "Strong@Pass123", new List<int> { 1, 2 });

            _mockUserRepo.Verify(r => r.AddAsync(It.Is<User>(u => u.Email == "new@test.com")), Times.Once);
            _mockUserRepo.Verify(r => r.UpdateUserRolesAsync(It.IsAny<int>(), It.Is<List<int>>(roles => roles.Count == 2)), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_NonExistentUser_ThrowsNotFoundException()
        {
            _mockUserRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);

            var dto = new UserDto { Id = 99, Name = "ناشناس" };

            await Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateAsync(dto, null, new List<int>()));
        }

        [Fact]
        public async Task UpdateAsync_ValidUser_UpdatesEntityAndRoles()
        {
            var existingUser = new User { Id = 10, Name = "نام قبلی", Email = "old@test.com", Password = "hashed_old_pass" };
            _mockUserRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(existingUser);

            var dto = new UserDto { Id = 10, Name = "نام جدید", Email = "old@test.com" };

            await _service.UpdateAsync(dto, null, new List<int> { 3 });

            _mockUserRepo.Verify(r => r.UpdateAsync(It.Is<User>(u => u.Id == 10 && u.Name == "نام جدید")), Times.Once);
            _mockUserRepo.Verify(r => r.UpdateUserRolesAsync(10, It.Is<List<int>>(roles => roles.Contains(3))), Times.Once);
        }

        [Fact]
        public async Task ExecuteBulkActionAsync_SingleDelete_CallsRepositoryDelete()
        {
            var existingUser = new User { Id = 12, Name = "کاربر حذف" };
            _mockUserRepo.Setup(r => r.GetByIdAsync(12)).ReturnsAsync(existingUser);

            await _service.ExecuteBulkActionAsync(new HashSet<int>(), "SingleDelete", 12);

            _mockUserRepo.Verify(r => r.DeleteAsync(12), Times.Once);
        }

        [Fact]
        public async Task ExecuteBulkActionAsync_BulkActivate_CallsRepositoryBulkUpdateStatus()
        {
            var userIds = new HashSet<int> { 1, 2, 3 };

            await _service.ExecuteBulkActionAsync(userIds, "Activate");

            _mockUserRepo.Verify(r => r.BulkUpdateStatusAsync(userIds, true), Times.Once);
        }

        [Fact]
        public async Task ExecuteBulkActionAsync_BulkDeactivate_CallsRepositoryBulkUpdateStatus()
        {
            var userIds = new HashSet<int> { 4, 5 };

            await _service.ExecuteBulkActionAsync(userIds, "Deactivate");

            _mockUserRepo.Verify(r => r.BulkUpdateStatusAsync(userIds, false), Times.Once);
        }
    }
}
