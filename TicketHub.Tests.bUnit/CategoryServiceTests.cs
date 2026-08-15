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
    public class CategoryServiceTests
    {
        private readonly Mock<IRepository<Category>> _mockCategoryRepo;
        private readonly Mock<IRepository<CategoryProject>> _mockCategoryProjectRepo;
        private readonly Mock<IRepository<Role>> _mockRoleRepo;
        private readonly Mock<ILogger<CategoryService>> _mockLogger;
        private readonly Mock<IValidator<CategoryDto>> _mockValidator;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly Mock<IPermissionService> _mockPermissionService;
        private readonly CategoryService _service;

        public CategoryServiceTests()
        {
            _mockCategoryRepo = new Mock<IRepository<Category>>();
            _mockCategoryProjectRepo = new Mock<IRepository<CategoryProject>>();
            _mockRoleRepo = new Mock<IRepository<Role>>();
            _mockLogger = new Mock<ILogger<CategoryService>>();
            _mockValidator = new Mock<IValidator<CategoryDto>>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _mockPermissionService = new Mock<IPermissionService>();

            // Default permission setup: Access granted
            _mockPermissionService.Setup(p => p.HasAccessAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>(), It.IsAny<PermissionType>()))
                .ReturnsAsync(true);

            // Default validator setup: Valid
            _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<CategoryDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _service = new CategoryService(
                _mockCategoryRepo.Object,
                _mockCategoryProjectRepo.Object,
                _mockRoleRepo.Object,
                _mockLogger.Object,
                _mockValidator.Object,
                _mockHttpContextAccessor.Object,
                _mockPermissionService.Object
            );
        }

        [Fact]
        public async Task GetAllAsync_ReturnsMappedCategoryDtos()
        {
            var categories = new List<Category>
            {
                new Category { Id = 1, Name = "Hardware", IsActive = true },
                new Category { Id = 2, Name = "Software", IsActive = false }
            };

            _mockCategoryRepo.Setup(r => r.GetAllWithIncludesAsync(It.IsAny<Expression<Func<Category, object?>>>()))
                .ReturnsAsync(categories);

            var result = await _service.GetAllAsync();

            result.Should().HaveCount(2);
            result.First().Name.Should().Be("Hardware");
            result.Last().IsActive.Should().BeFalse();
        }

        [Fact]
        public async Task GetByIdAsync_CategoryExists_ReturnsCategoryDto()
        {
            var categories = new List<Category>
            {
                new Category { Id = 10, Name = "Network", IsActive = true }
            };

            _mockCategoryRepo.Setup(r => r.GetAllWithIncludesAsync(It.IsAny<Expression<Func<Category, object?>>>()))
                .ReturnsAsync(categories);

            var result = await _service.GetByIdAsync(10);

            result.Should().NotBeNull();
            result!.Name.Should().Be("Network");
        }

        [Fact]
        public async Task GetByIdAsync_CategoryNotFound_ThrowsNotFoundException()
        {
            _mockCategoryRepo.Setup(r => r.GetAllWithIncludesAsync(It.IsAny<Expression<Func<Category, object?>>>()))
                .ReturnsAsync(new List<Category>());

            Func<Task> act = () => _service.GetByIdAsync(999);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task AddAsync_WithValidDto_AddsToRepository()
        {
            var dto = new CategoryDto { Name = "Security", IsActive = true, ProjectIds = new List<int> { 1, 2 } };

            var result = await _service.AddAsync(dto);

            _mockCategoryRepo.Verify(r => r.AddAsync(It.Is<Category>(c => c.Name == "Security")), Times.Once);
            _mockCategoryProjectRepo.Verify(r => r.AddAsync(It.IsAny<CategoryProject>()), Times.Exactly(2));
        }

        [Fact]
        public async Task AddAsync_ValidationFails_ThrowsValidationException()
        {
            var dto = new CategoryDto { Name = "" };
            var failures = new List<ValidationFailure> { new ValidationFailure("Name", "نام دسته‌بندی الزامی است") };
            _mockValidator.Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(failures));

            Func<Task> act = () => _service.AddAsync(dto);

            await act.Should().ThrowAsync<ValidationException>();
            _mockCategoryRepo.Verify(r => r.AddAsync(It.IsAny<Category>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_CategoryExists_UpdatesRepository()
        {
            var existing = new Category { Id = 1, Name = "Old" };
            _mockCategoryRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
            _mockCategoryProjectRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<CategoryProject>());

            var dto = new CategoryDto { Id = 1, Name = "New", ProjectIds = new List<int> { 5 } };

            await _service.UpdateAsync(dto);

            _mockCategoryRepo.Verify(r => r.UpdateAsync(It.Is<Category>(c => c.Id == 1 && c.Name == "New")), Times.Once);
            _mockCategoryProjectRepo.Verify(r => r.AddAsync(It.Is<CategoryProject>(cp => cp.ProjectId == 5)), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_CategoryNotFound_ThrowsNotFoundException()
        {
            _mockCategoryRepo.Setup(r => r.GetByIdAsync(404)).ReturnsAsync((Category?)null);

            var dto = new CategoryDto { Id = 404, Name = "Missing" };

            Func<Task> act = () => _service.UpdateAsync(dto);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task DeleteAsync_CategoryExists_DeletesFromRepository()
        {
            var existing = new Category { Id = 8, Name = "ToDelete" };
            _mockCategoryRepo.Setup(r => r.GetByIdAsync(8)).ReturnsAsync(existing);
            _mockCategoryProjectRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<CategoryProject>());

            await _service.DeleteAsync(new CategoryDto { Id = 8 });

            _mockCategoryRepo.Verify(r => r.DeleteAsync(8), Times.Once);
        }

        [Fact]
        public async Task DeleteRangeAsync_DeletesCategoriesAndRelations()
        {
            var categories = new List<Category>
            {
                new Category { Id = 1, Name = "C1" },
                new Category { Id = 2, Name = "C2" }
            };
            _mockCategoryRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(categories);
            _mockCategoryProjectRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<CategoryProject>());

            await _service.DeleteRangeAsync(new[] { 1, 2 });

            _mockCategoryRepo.Verify(r => r.DeleteRangeAsync(It.Is<IEnumerable<Category>>(c => c.Count() == 2)), Times.Once);
        }

        [Fact]
        public async Task UpdateCategoriesStatusAsync_UpdatesStatusForMultiple()
        {
            var categories = new List<Category>
            {
                new Category { Id = 1, Name = "C1", IsActive = false },
                new Category { Id = 2, Name = "C2", IsActive = false }
            };
            _mockCategoryRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(categories);

            await _service.UpdateCategoriesStatusAsync(new[] { 1, 2 }, true);

            _mockCategoryRepo.Verify(r => r.UpdateAsync(It.Is<Category>(c => c.IsActive)), Times.Exactly(2));
        }

        [Fact]
        public async Task GetCategoriesByProjectIdAsync_FiltersByProject()
        {
            var categories = new List<Category>
            {
                new Category
                {
                    Id = 1,
                    Name = "DevOps",
                    IsActive = true,
                    CategoryProjects = new List<CategoryProject> { new CategoryProject { CategoryId = 1, ProjectId = 100 } }
                },
                new Category
                {
                    Id = 2,
                    Name = "UI/UX",
                    IsActive = true,
                    CategoryProjects = new List<CategoryProject> { new CategoryProject { CategoryId = 2, ProjectId = 200 } }
                }
            };

            _mockCategoryRepo.Setup(r => r.GetAllWithIncludesAsync(It.IsAny<Expression<Func<Category, object?>>>()))
                .ReturnsAsync(categories);

            var result = await _service.GetCategoriesByProjectIdAsync(100);

            result.Should().HaveCount(1);
            result.First().Name.Should().Be("DevOps");
        }
    }
}
