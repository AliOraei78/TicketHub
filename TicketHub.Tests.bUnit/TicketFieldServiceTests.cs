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
    public class TicketFieldServiceTests
    {
        private readonly Mock<IRepository<TicketField>> _mockTicketFieldRepo;
        private readonly Mock<IRepository<FieldCategory>> _mockFieldCategoryRepo;
        private readonly Mock<ILogger<TicketFieldService>> _mockLogger;
        private readonly Mock<IValidator<TicketFieldDto>> _mockValidator;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly Mock<IPermissionService> _mockPermissionService;
        private readonly TicketFieldService _service;

        public TicketFieldServiceTests()
        {
            _mockTicketFieldRepo = new Mock<IRepository<TicketField>>();
            _mockFieldCategoryRepo = new Mock<IRepository<FieldCategory>>();
            _mockLogger = new Mock<ILogger<TicketFieldService>>();
            _mockValidator = new Mock<IValidator<TicketFieldDto>>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _mockPermissionService = new Mock<IPermissionService>();

            // Default permission setup: Access granted
            _mockPermissionService.Setup(p => p.HasAccessAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>(), It.IsAny<PermissionType>()))
                .ReturnsAsync(true);

            // Default validator setup: Valid
            _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<TicketFieldDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _service = new TicketFieldService(
                _mockTicketFieldRepo.Object,
                _mockFieldCategoryRepo.Object,
                _mockLogger.Object,
                _mockValidator.Object,
                _mockHttpContextAccessor.Object,
                _mockPermissionService.Object
            );
        }

        [Fact]
        public async Task GetAllAsync_ReturnsMappedTicketFieldDtos()
        {
            var fields = new List<TicketField>
            {
                new TicketField { Id = 1, Name = "Phone", SortOrder = 1, IsActive = true },
                new TicketField { Id = 2, Name = "Serial", SortOrder = 2, IsActive = false }
            };

            _mockTicketFieldRepo.Setup(r => r.GetAllWithIncludesAsync(It.IsAny<Expression<Func<TicketField, object?>>[]>()))
                .ReturnsAsync(fields);

            var result = await _service.GetAllAsync();

            result.Should().HaveCount(2);
            result.First().Name.Should().Be("Phone");
            result.Last().IsActive.Should().BeFalse();
        }

        [Fact]
        public async Task GetByIdAsync_FieldExists_ReturnsTicketFieldDto()
        {
            var fields = new List<TicketField>
            {
                new TicketField { Id = 5, Name = "IMEI", SortOrder = 1 }
            };

            _mockTicketFieldRepo.Setup(r => r.GetAllWithIncludesAsync(It.IsAny<Expression<Func<TicketField, object?>>[]>()))
                .ReturnsAsync(fields);

            var result = await _service.GetByIdAsync(5);

            result.Should().NotBeNull();
            result!.Name.Should().Be("IMEI");
        }

        [Fact]
        public async Task GetByIdAsync_FieldNotFound_ThrowsNotFoundException()
        {
            _mockTicketFieldRepo.Setup(r => r.GetAllWithIncludesAsync(It.IsAny<Expression<Func<TicketField, object?>>[]>()))
                .ReturnsAsync(new List<TicketField>());

            Func<Task> act = () => _service.GetByIdAsync(999);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task AddAsync_WithValidDto_AddsToRepository()
        {
            var dto = new TicketFieldDto { Name = "LicenseKey", CategoryIds = new List<int> { 1, 3 } };

            var result = await _service.AddAsync(dto);

            _mockTicketFieldRepo.Verify(r => r.AddAsync(It.Is<TicketField>(f => f.Name == "LicenseKey")), Times.Once);
            _mockFieldCategoryRepo.Verify(r => r.AddAsync(It.IsAny<FieldCategory>()), Times.Exactly(2));
        }

        [Fact]
        public async Task AddAsync_ValidationFails_ThrowsValidationException()
        {
            var dto = new TicketFieldDto { Name = "" };
            var failures = new List<ValidationFailure> { new ValidationFailure("Name", "نام فیلد الزامی است") };
            _mockValidator.Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(failures));

            Func<Task> act = () => _service.AddAsync(dto);

            await act.Should().ThrowAsync<ValidationException>();
            _mockTicketFieldRepo.Verify(r => r.AddAsync(It.IsAny<TicketField>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_FieldExists_UpdatesRepository()
        {
            var existing = new TicketField { Id = 1, Name = "Old" };
            _mockTicketFieldRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
            _mockFieldCategoryRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<FieldCategory>());

            var dto = new TicketFieldDto { Id = 1, Name = "New", CategoryIds = new List<int> { 10 } };

            await _service.UpdateAsync(dto);

            _mockTicketFieldRepo.Verify(r => r.UpdateAsync(It.Is<TicketField>(f => f.Id == 1 && f.Name == "New")), Times.Once);
            _mockFieldCategoryRepo.Verify(r => r.AddAsync(It.Is<FieldCategory>(fc => fc.CategoryId == 10)), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_FieldNotFound_ThrowsNotFoundException()
        {
            _mockTicketFieldRepo.Setup(r => r.GetByIdAsync(404)).ReturnsAsync((TicketField?)null);

            var dto = new TicketFieldDto { Id = 404, Name = "Missing" };

            Func<Task> act = () => _service.UpdateAsync(dto);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task DeleteAsync_FieldExists_DeletesFromRepository()
        {
            var existing = new TicketField { Id = 12, Name = "ToDelete" };
            _mockTicketFieldRepo.Setup(r => r.GetByIdAsync(12)).ReturnsAsync(existing);
            _mockFieldCategoryRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<FieldCategory>());

            await _service.DeleteAsync(new TicketFieldDto { Id = 12 });

            _mockTicketFieldRepo.Verify(r => r.DeleteAsync(12), Times.Once);
        }

        [Fact]
        public async Task DeleteRangeAsync_DeletesMultipleFields()
        {
            var fields = new List<TicketField>
            {
                new TicketField { Id = 1, Name = "F1" },
                new TicketField { Id = 2, Name = "F2" }
            };
            _mockTicketFieldRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(fields);
            _mockFieldCategoryRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<FieldCategory>());

            await _service.DeleteRangeAsync(new[] { 1, 2 });

            _mockTicketFieldRepo.Verify(r => r.DeleteRangeAsync(It.Is<IEnumerable<TicketField>>(f => f.Count() == 2)), Times.Once);
        }

        [Fact]
        public async Task UpdateTicketFieldsStatusAsync_UpdatesStatusForMultiple()
        {
            var fields = new List<TicketField>
            {
                new TicketField { Id = 1, Name = "F1", IsActive = false },
                new TicketField { Id = 2, Name = "F2", IsActive = false }
            };
            _mockTicketFieldRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(fields);

            await _service.UpdateTicketFieldsStatusAsync(new[] { 1, 2 }, true);

            _mockTicketFieldRepo.Verify(r => r.UpdateAsync(It.Is<TicketField>(f => f.IsActive)), Times.Exactly(2));
        }

        [Fact]
        public async Task GetFieldsByCategoryIdAsync_FiltersByCategory()
        {
            var fields = new List<TicketField>
            {
                new TicketField
                {
                    Id = 1,
                    Name = "DevVersion",
                    IsActive = true,
                    SortOrder = 1,
                    FieldCategories = new List<FieldCategory> { new FieldCategory { TicketFieldId = 1, CategoryId = 10 } }
                }
            };

            _mockTicketFieldRepo.Setup(r => r.GetAllWithIncludesAsync(It.IsAny<Expression<Func<TicketField, object?>>[]>()))
                .ReturnsAsync(fields);

            var result = await _service.GetFieldsByCategoryIdAsync(10);

            result.Should().HaveCount(1);
            result.First().Name.Should().Be("DevVersion");
        }
    }
}
