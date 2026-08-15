using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
    public class ProjectServiceTests
    {
        private readonly Mock<IProjectRepository> _mockProjectRepo;
        private readonly Mock<IRepository<Workflow>> _mockWorkflowRepo;
        private readonly Mock<IRepository<RoleProject>> _mockRoleProjectRepo;
        private readonly Mock<IRepository<Role>> _mockRoleRepo;
        private readonly Mock<ILogger<ProjectService>> _mockLogger;
        private readonly Mock<IValidator<ProjectDto>> _mockValidator;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly Mock<IPermissionService> _mockPermissionService;

        private readonly ProjectService _service;

        public ProjectServiceTests()
        {
            _mockProjectRepo = new Mock<IProjectRepository>();
            _mockWorkflowRepo = new Mock<IRepository<Workflow>>();
            _mockRoleProjectRepo = new Mock<IRepository<RoleProject>>();
            _mockRoleRepo = new Mock<IRepository<Role>>();
            _mockLogger = new Mock<ILogger<ProjectService>>();
            _mockValidator = new Mock<IValidator<ProjectDto>>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _mockPermissionService = new Mock<IPermissionService>();

            // Setup default validator passing
            _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<ProjectDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            // Setup default permission passing
            _mockPermissionService.Setup(p => p.HasAccessAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>(), It.IsAny<PermissionType>()))
                .ReturnsAsync(true);

            _service = new ProjectService(
                _mockProjectRepo.Object,
                _mockWorkflowRepo.Object,
                _mockRoleProjectRepo.Object,
                _mockRoleRepo.Object,
                _mockLogger.Object,
                _mockValidator.Object,
                _mockHttpContextAccessor.Object,
                _mockPermissionService.Object
            );
        }

        [Fact]
        public async Task GetProjectsAsync_ReturnsMappedProjectDtos()
        {
            // Arrange
            var projects = new List<Project>
            {
                new Project { Id = 1, Name = "Alpha", IsActive = true, RoleProjects = new List<RoleProject>() },
                new Project { Id = 2, Name = "Beta", IsActive = false, RoleProjects = new List<RoleProject>() }
            };

            _mockProjectRepo.Setup(r => r.GetAllWithIncludesAsync(It.IsAny<Expression<Func<Project, object?>>[]>()))
                .ReturnsAsync(projects);

            // Act
            var result = await _service.GetProjectsAsync();

            // Assert
            result.Should().NotBeNull();
            result.Count().Should().Be(2);
            result.First().Name.Should().Be("Alpha");
        }

        [Fact]
        public async Task GetWorkflowsAsync_ReturnsMappedWorkflowDtos()
        {
            // Arrange
            var workflows = new List<Workflow>
            {
                new Workflow { Id = 10, Name = "Main Workflow", IsActive = true }
            };

            _mockWorkflowRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(workflows);

            // Act
            var result = await _service.GetWorkflowsAsync();

            // Assert
            result.Should().NotBeNull();
            result.Count().Should().Be(1);
            result.First().Name.Should().Be("Main Workflow");
        }

        [Fact]
        public async Task AddProjectAsync_WhenValidationFails_ThrowsValidationException()
        {
            // Arrange
            var failures = new List<ValidationFailure>
            {
                new ValidationFailure("Name", "نام پروژه الزامی است.")
            };
            _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<ProjectDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(failures));

            var dto = new ProjectDto { Name = "" };

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(() => _service.AddProjectAsync(dto));
            _mockProjectRepo.Verify(r => r.AddAsync(It.IsAny<Project>()), Times.Never);
        }

        [Fact]
        public async Task AddProjectAsync_WhenUnauthorized_ThrowsForbiddenException()
        {
            // Arrange
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "user1") }, "TestAuth"));
            var httpContext = new DefaultHttpContext { User = user };
            _mockHttpContextAccessor.Setup(h => h.HttpContext).Returns(httpContext);

            _mockPermissionService.Setup(p => p.HasAccessAsync(user, "/projects", PermissionType.SystemSection))
                .ReturnsAsync(false);

            var dto = new ProjectDto { Name = "پروژه جدید" };

            // Act & Assert
            await Assert.ThrowsAsync<ForbiddenException>(() => _service.AddProjectAsync(dto));
            _mockProjectRepo.Verify(r => r.AddAsync(It.IsAny<Project>()), Times.Never);
        }

        [Fact]
        public async Task AddProjectAsync_ValidDto_AddsProjectAndRoleProjects()
        {
            // Arrange
            var dto = new ProjectDto
            {
                Name = "پروژه سایبری",
                Description = "توضیحات",
                IsActive = true,
                RoleIds = new List<int> { 1, 2 }
            };

            // Act
            await _service.AddProjectAsync(dto);

            // Assert
            _mockProjectRepo.Verify(r => r.AddAsync(It.Is<Project>(p => p.Name == "پروژه سایبری")), Times.Once);
            _mockRoleProjectRepo.Verify(r => r.AddAsync(It.IsAny<RoleProject>()), Times.Exactly(2));
        }

        [Fact]
        public async Task UpdateProjectAsync_NonExistentProject_ThrowsNotFoundException()
        {
            // Arrange
            _mockProjectRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Project?)null);

            var dto = new ProjectDto { Id = 99, Name = "ناشناس" };

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateProjectAsync(dto));
            _mockProjectRepo.Verify(r => r.UpdateAsync(It.IsAny<Project>()), Times.Never);
        }

        [Fact]
        public async Task UpdateProjectAsync_ValidDto_UpdatesEntityAndRefreshesRoleProjects()
        {
            // Arrange
            var existingProject = new Project { Id = 5, Name = "پروژه قبلی" };
            _mockProjectRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(existingProject);

            var oldRoleProjects = new List<RoleProject>
            {
                new RoleProject { Id = 1, ProjectId = 5, RoleId = 10 },
                new RoleProject { Id = 2, ProjectId = 8, RoleId = 20 }
            };
            _mockRoleProjectRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(oldRoleProjects);

            var dto = new ProjectDto
            {
                Id = 5,
                Name = "پروژه ویرایش شده",
                RoleIds = new List<int> { 30, 40 }
            };

            // Act
            await _service.UpdateProjectAsync(dto);

            // Assert
            _mockProjectRepo.Verify(r => r.UpdateAsync(It.Is<Project>(p => p.Id == 5 && p.Name == "پروژه ویرایش شده")), Times.Once);
            _mockRoleProjectRepo.Verify(r => r.DeleteRangeAsync(It.Is<IEnumerable<RoleProject>>(list => list.Count() == 1)), Times.Once);
            _mockRoleProjectRepo.Verify(r => r.AddAsync(It.IsAny<RoleProject>()), Times.Exactly(2));
        }

        [Fact]
        public async Task DeleteProjectAsync_NonExistentProject_ThrowsNotFoundException()
        {
            // Arrange
            _mockProjectRepo.Setup(r => r.GetByIdAsync(88)).ReturnsAsync((Project?)null);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteProjectAsync(88));
            _mockProjectRepo.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task DeleteProjectAsync_ValidId_DeletesRoleProjectsAndProject()
        {
            // Arrange
            var existingProject = new Project { Id = 7, Name = "پروژه برای حذف" };
            _mockProjectRepo.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(existingProject);

            var roleProjects = new List<RoleProject>
            {
                new RoleProject { Id = 100, ProjectId = 7, RoleId = 1 },
                new RoleProject { Id = 101, ProjectId = 7, RoleId = 2 }
            };
            _mockRoleProjectRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(roleProjects);

            // Act
            await _service.DeleteProjectAsync(7);

            // Assert
            _mockRoleProjectRepo.Verify(r => r.DeleteRangeAsync(It.Is<IEnumerable<RoleProject>>(list => list.Count() == 2)), Times.Once);
            _mockProjectRepo.Verify(r => r.DeleteAsync(7), Times.Once);
        }

        [Fact]
        public async Task GetProjectsByUserRolesAsync_WhenAdmin_ReturnsAllActiveProjects()
        {
            // Arrange
            var projects = new List<Project>
            {
                new Project { Id = 1, Name = "P1", IsActive = true, RoleProjects = new List<RoleProject>() },
                new Project { Id = 2, Name = "P2", IsActive = false, RoleProjects = new List<RoleProject>() }
            };
            _mockProjectRepo.Setup(r => r.GetAllWithIncludesAsync(It.IsAny<Expression<Func<Project, object?>>[]>()))
                .ReturnsAsync(projects);

            // Act
            var result = await _service.GetProjectsByUserRolesAsync(new[] { "مدیر سیستم" });

            // Assert
            result.Should().NotBeNull();
            result.Count.Should().Be(1);
            result.First().Id.Should().Be(1);
        }

        [Fact]
        public async Task GetProjectsByUserRolesAsync_RegularUser_FiltersByAssignedRoles()
        {
            // Arrange
            var roles = new List<Role>
            {
                new Role { Id = 10, Name = "کارشناس پشتیبانی" }
            };
            _mockRoleRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(roles);

            var projects = new List<Project>
            {
                new Project
                {
                    Id = 1,
                    Name = "پروژه بدون محدودیت نقش",
                    IsActive = true,
                    RoleProjects = new List<RoleProject>()
                },
                new Project
                {
                    Id = 2,
                    Name = "پروژه دارای نقش منطبق",
                    IsActive = true,
                    RoleProjects = new List<RoleProject> { new RoleProject { ProjectId = 2, RoleId = 10 } }
                },
                new Project
                {
                    Id = 3,
                    Name = "پروژه نقش نامنطبق",
                    IsActive = true,
                    RoleProjects = new List<RoleProject> { new RoleProject { ProjectId = 3, RoleId = 99 } }
                }
            };

            _mockProjectRepo.Setup(r => r.GetAllWithIncludesAsync(It.IsAny<Expression<Func<Project, object?>>[]>()))
                .ReturnsAsync(projects);

            // Act
            var result = await _service.GetProjectsByUserRolesAsync(new[] { "کارشناس پشتیبانی" });

            // Assert
            result.Count.Should().Be(2);
            result.Select(p => p.Id).Should().Contain(new[] { 1, 2 });
            result.Select(p => p.Id).Should().NotContain(3);
        }
    }
}
