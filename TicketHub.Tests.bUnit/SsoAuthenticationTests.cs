using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TicketHub.Application.Common.Models;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Application.Models;
using TicketHub.Application.Services;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;
using TicketHub.Web.Security;
using Xunit;

namespace TicketHub.Tests.bUnit
{
    public class SsoAuthenticationTests
    {
        private readonly Mock<IUserRepository> _mockUserRepo;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<IRepository<Role>> _mockRoleRepo;
        private readonly Mock<ILogger<UserService>> _mockLogger;
        private readonly Mock<IValidator<UserDto>> _mockValidator;
        private readonly Mock<IHttpContextAccessor> _mockHttpAccessor;
        private readonly Mock<IPermissionService> _mockPermissionService;

        public SsoAuthenticationTests()
        {
            _mockUserRepo = new Mock<IUserRepository>();
            _mockEmailService = new Mock<IEmailService>();
            _mockRoleRepo = new Mock<IRepository<Role>>();
            _mockLogger = new Mock<ILogger<UserService>>();
            _mockValidator = new Mock<IValidator<UserDto>>();
            _mockHttpAccessor = new Mock<IHttpContextAccessor>();
            _mockPermissionService = new Mock<IPermissionService>();
        }

        private UserService CreateService()
        {
            return new UserService(
                _mockUserRepo.Object,
                _mockEmailService.Object,
                _mockRoleRepo.Object,
                _mockLogger.Object,
                _mockValidator.Object,
                _mockHttpAccessor.Object,
                _mockPermissionService.Object);
        }

        [Fact]
        public async Task ProcessExternalLoginAsync_WithExistingLinkedUser_ReturnsSuccessAndRoles()
        {
            // Arrange
            var user = new User
            {
                Id = 42,
                Name = "John Doe",
                Email = "john@enterprise.com",
                ExternalProvider = "Keycloak",
                ExternalSubjectId = "sub-12345",
                IsActive = true,
                IsConfirmed = true,
                UserRoles = new List<UserRole>
                {
                    new UserRole { Role = new Role { Id = 1, Name = "ادمین" } },
                    new UserRole { Role = new Role { Id = 2, Name = "کارشناس" } }
                }
            };

            _mockUserRepo.Setup(r => r.GetByExternalProviderAsync("Keycloak", "sub-12345"))
                .ReturnsAsync(user);

            var service = CreateService();

            // Act
            var result = await service.ProcessExternalLoginAsync("Keycloak", "sub-12345", "john@enterprise.com", "John Doe");

            // Assert
            Assert.True(result.Success);
            Assert.Equal(42, result.UserId);
            Assert.Equal("John Doe", result.Name);
            Assert.Equal("john@enterprise.com", result.Email);
            Assert.Contains("ادمین", result.Roles);
            Assert.Contains("کارشناس", result.Roles);
            _mockUserRepo.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task ProcessExternalLoginAsync_WithExistingUserByEmail_LinksProviderAndReturnsSuccess()
        {
            // Arrange
            var existingUser = new User
            {
                Id = 10,
                Name = "Alice Smith",
                Email = "alice@company.com",
                ExternalProvider = null,
                ExternalSubjectId = null,
                IsActive = true,
                IsConfirmed = false,
                UserRoles = new List<UserRole>
                {
                    new UserRole { Role = new Role { Id = 3, Name = "کاربر" } }
                }
            };

            _mockUserRepo.Setup(r => r.GetByExternalProviderAsync("Google", "google-sub-999"))
                .ReturnsAsync((User?)null);
            _mockUserRepo.Setup(r => r.GetByEmailAsync("alice@company.com"))
                .ReturnsAsync(existingUser);
            _mockUserRepo.Setup(r => r.UpdateAsync(It.IsAny<User>()))
                .Returns(Task.CompletedTask);

            var service = CreateService();

            // Act
            var result = await service.ProcessExternalLoginAsync("Google", "google-sub-999", "alice@company.com", "Alice Smith");

            // Assert
            Assert.True(result.Success);
            Assert.Equal(10, result.UserId);
            Assert.Equal("Google", existingUser.ExternalProvider);
            Assert.Equal("google-sub-999", existingUser.ExternalSubjectId);
            Assert.True(existingUser.IsConfirmed);
            _mockUserRepo.Verify(r => r.UpdateAsync(existingUser), Times.Once);
        }

        [Fact]
        public async Task ProcessExternalLoginAsync_WithNewUser_AutoProvisionsWithDefaultRole()
        {
            // Arrange
            _mockUserRepo.Setup(r => r.GetByExternalProviderAsync("Microsoft", "ms-sub-777"))
                .ReturnsAsync((User?)null);
            _mockUserRepo.Setup(r => r.GetByEmailAsync("newbie@enterprise.org"))
                .ReturnsAsync((User?)null);

            var defaultRole = new Role { Id = 5, Name = "کاربر" };
            _mockRoleRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<Role> { defaultRole });

            User? createdUser = null;
            _mockUserRepo.Setup(r => r.AddAsync(It.IsAny<User>()))
                .Callback<User>(u =>
                {
                    u.Id = 88;
                    createdUser = u;
                })
                .Returns(Task.CompletedTask);

            _mockUserRepo.Setup(r => r.UpdateUserRolesAsync(88, It.IsAny<List<int>>()))
                .Returns(Task.CompletedTask);

            _mockUserRepo.Setup(r => r.GetByEmailAsync("newbie@enterprise.org"))
                .ReturnsAsync(() => createdUser != null ? new User
                {
                    Id = 88,
                    Name = createdUser.Name,
                    Email = createdUser.Email,
                    IsActive = true,
                    IsConfirmed = true,
                    UserRoles = new List<UserRole> { new UserRole { Role = defaultRole } }
                } : null);

            var service = CreateService();

            // Act
            var result = await service.ProcessExternalLoginAsync("Microsoft", "ms-sub-777", "newbie@enterprise.org", "Newbie User");

            // Assert
            Assert.True(result.Success);
            Assert.Equal(88, result.UserId);
            Assert.Equal("Newbie User", result.Name);
            Assert.Equal("newbie@enterprise.org", result.Email);
            Assert.Contains("کاربر", result.Roles);
            _mockUserRepo.Verify(r => r.AddAsync(It.Is<User>(u =>
                u.Email == "newbie@enterprise.org" &&
                u.ExternalProvider == "Microsoft" &&
                u.ExternalSubjectId == "ms-sub-777" &&
                u.IsConfirmed &&
                u.IsActive)), Times.Once);
        }

        [Fact]
        public async Task ProcessExternalLoginAsync_WithEmptyEmail_ReturnsFailure()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = await service.ProcessExternalLoginAsync("Keycloak", "sub-123", "", "Test");

            // Assert
            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
        }

        [Fact]
        public async Task ProcessExternalLoginAsync_WithInactiveUser_ReturnsAccountDisabled()
        {
            // Arrange
            var inactiveUser = new User
            {
                Id = 15,
                Name = "Blocked User",
                Email = "blocked@enterprise.com",
                ExternalProvider = "Keycloak",
                ExternalSubjectId = "sub-blocked",
                IsActive = false,
                IsConfirmed = true
            };

            _mockUserRepo.Setup(r => r.GetByExternalProviderAsync("Keycloak", "sub-blocked"))
                .ReturnsAsync(inactiveUser);

            var service = CreateService();

            // Act
            var result = await service.ProcessExternalLoginAsync("Keycloak", "sub-blocked", "blocked@enterprise.com", "Blocked User");

            // Assert
            Assert.False(result.Success);
            Assert.Contains("غیرفعال", result.ErrorMessage);
        }

        [Fact]
        public void SsoSettings_DefaultValues_AreProperlyConfigured()
        {
            // Act
            var settings = new SsoSettings();

            // Assert
            Assert.False(settings.Enabled);
            Assert.Equal("OIDC", settings.ProviderName);
            Assert.Equal("ورود یکپارچه سازمانی (SSO)", settings.DisplayName);
            Assert.Equal("code", settings.ResponseType);
            Assert.Equal("/signin-oidc", settings.CallbackPath);
            Assert.Equal("/signout-callback-oidc", settings.SignedOutCallbackPath);
            Assert.Equal("کاربر", settings.DefaultRole);
            Assert.True(settings.AutoProvisionUsers);
            Assert.Contains("openid", settings.Scopes);
            Assert.Contains("email", settings.Scopes);
        }

        [Fact]
        public void SsoSettings_ConfigurationBinding_BindsCorrectly()
        {
            // Arrange
            var inMemorySettings = new Dictionary<string, string?>
            {
                { "SSO:Enabled", "true" },
                { "SSO:ProviderName", "Keycloak" },
                { "SSO:DisplayName", "ورود سازمانی شرکت" },
                { "SSO:Authority", "https://sso.mycorp.local/realms/corp" },
                { "SSO:ClientId", "tickethub-app" },
                { "SSO:ClientSecret", "secret123" },
                { "SSO:RequireHttpsMetadata", "false" },
                { "SSO:AutoProvisionUsers", "true" }
            };

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            // Act
            var settings = configuration.GetSection(SsoSettings.SectionName).Get<SsoSettings>();

            // Assert
            Assert.NotNull(settings);
            Assert.True(settings.Enabled);
            Assert.Equal("Keycloak", settings.ProviderName);
            Assert.Equal("ورود سازمانی شرکت", settings.DisplayName);
            Assert.Equal("https://sso.mycorp.local/realms/corp", settings.Authority);
            Assert.Equal("tickethub-app", settings.ClientId);
            Assert.Equal("secret123", settings.ClientSecret);
            Assert.False(settings.RequireHttpsMetadata);
            Assert.True(settings.AutoProvisionUsers);
        }

        [Fact]
        public void AddTicketHubAuthentication_RegistersAuthenticationServicesSuccessfully()
        {
            // Arrange
            var inMemorySettings = new Dictionary<string, string?>
            {
                { "SSO:Enabled", "false" }
            };

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var services = new ServiceCollection();

            // Act
            services.AddTicketHubAuthentication(configuration);
            var provider = services.BuildServiceProvider();

            // Assert
            var ssoOptions = provider.GetService<IOptions<SsoSettings>>();
            Assert.NotNull(ssoOptions);
            Assert.False(ssoOptions.Value.Enabled);
        }
    }
}
