using Mapster;
using Microsoft.Extensions.Logging;
using FluentValidation;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Core.Common.Exceptions;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;
using ValidationException = TicketHub.Core.Common.Exceptions.ValidationException;

using Microsoft.AspNetCore.Http;
using TicketHub.Application.Enums;

namespace TicketHub.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly IRepository<Category> _categoryRepo;
    private readonly IRepository<CategoryProject> _categoryProjectRepo;
    private readonly IRepository<Role> _roleRepo;
    private readonly ILogger<CategoryService> _logger;
    private readonly IValidator<CategoryDto> _validator;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IPermissionService _permissionService;

    public CategoryService(
        IRepository<Category> categoryRepo,
        IRepository<CategoryProject> categoryProjectRepo,
        IRepository<Role> roleRepo,
        ILogger<CategoryService> logger,
        IValidator<CategoryDto> validator,
        IHttpContextAccessor httpContextAccessor,
        IPermissionService permissionService)
    {
        _categoryRepo = categoryRepo;
        _categoryProjectRepo = categoryProjectRepo;
        _roleRepo = roleRepo;
        _logger = logger;
        _validator = validator;
        _httpContextAccessor = httpContextAccessor;
        _permissionService = permissionService;
    }

    private async Task EnsurePermissionAsync(PermissionType minType, string message)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user != null && user.Identity?.IsAuthenticated == true)
        {
            if (!await _permissionService.HasAccessAsync(user, "/settings/categories", minType))
            {
                throw new ForbiddenException(message);
            }
        }
    }

    private async Task ValidateDtoAsync(CategoryDto dto)
    {
        var validationResult = await _validator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );

            throw new ValidationException(errors);
        }
    }

    public async Task<List<CategoryDto>> GetAllAsync()
    {
        _logger.LogInformation("شروع دریافت تمامی دسته‌بندی‌ها از دیتابیس.");

        var categories = await _categoryRepo.GetAllWithIncludesAsync(c => c.CategoryProjects);

        _logger.LogInformation("تعداد {Count} دسته‌بندی با موفقیت دریافت شد.", categories.Count());
        return categories.Adapt<List<CategoryDto>>();
    }

    public async Task<CategoryDto?> GetByIdAsync(int id)
    {
        _logger.LogInformation("جستجوی دسته‌بندی با شناسه: {Id}", id);

        var categories = await _categoryRepo.GetAllWithIncludesAsync(c => c.CategoryProjects);

        var category = categories.FirstOrDefault(c => c.Id == id);

        if (category == null)
            throw new NotFoundException("دسته‌بندی", id);

        _logger.LogInformation("دسته‌بندی با شناسه {Id} با موفقیت پیدا شد.", id);
        return category.Adapt<CategoryDto>();
    }

    public async Task<CategoryDto> AddAsync(CategoryDto dto)
    {
        await EnsurePermissionAsync(PermissionType.SystemSection, "شما دسترسی لازم برای ایجاد دسته‌بندی را ندارید.");
        await ValidateDtoAsync(dto);

        _logger.LogInformation("شروع فرآیند ایجاد دسته‌بندی جدید.");

        var entity = dto.Adapt<Category>();
        entity.CreatedAt = DateTime.UtcNow;

        await _categoryRepo.AddAsync(entity);
        _logger.LogInformation("دسته‌بندی پایه با شناسه {Id} ایجاد شد.", entity.Id);

        if (dto.ProjectIds?.Any() == true)
        {
            _logger.LogInformation("افزودن {Count} پروژه به دسته‌بندی {Id}.", dto.ProjectIds.Count, entity.Id);
            foreach (var projectId in dto.ProjectIds)
            {
                await _categoryProjectRepo.AddAsync(new CategoryProject
                {
                    CategoryId = entity.Id,
                    ProjectId = projectId
                });
            }
        }

        _logger.LogInformation("فرآیند ایجاد دسته‌بندی {Id} با موفقیت به پایان رسید.", entity.Id);
        return entity.Adapt<CategoryDto>();
    }

    public async Task UpdateAsync(CategoryDto dto)
    {
        await EnsurePermissionAsync(PermissionType.SystemSection, "شما دسترسی لازم برای ویرایش دسته‌بندی را ندارید.");
        await ValidateDtoAsync(dto);

        _logger.LogInformation("شروع فرآیند بروزرسانی دسته‌بندی با شناسه {Id}.", dto.Id);

        var existingCategory = await _categoryRepo.GetByIdAsync(dto.Id);
        if (existingCategory == null)
            throw new NotFoundException("دسته‌بندی", dto.Id);

        var entity = dto.Adapt<Category>();
        await _categoryRepo.UpdateAsync(entity);

        // پروژه‌ها
        var allCategoryProjects = await _categoryProjectRepo.GetAllAsync();
        var oldProjects = allCategoryProjects.Where(cp => cp.CategoryId == entity.Id).ToList();

        if (oldProjects.Any())
        {
            _logger.LogInformation("حذف {Count} پروژه قدیمی از دسته‌بندی {Id}.", oldProjects.Count, entity.Id);
            await _categoryProjectRepo.DeleteRangeAsync(oldProjects);
        }

        if (dto.ProjectIds?.Any() == true)
        {
            _logger.LogInformation("ثبت {Count} پروژه جدید برای دسته‌بندی {Id}.", dto.ProjectIds.Count, entity.Id);
            foreach (var projectId in dto.ProjectIds)
            {
                await _categoryProjectRepo.AddAsync(new CategoryProject
                {
                    CategoryId = entity.Id,
                    ProjectId = projectId
                });
            }
        }

        _logger.LogInformation("بروزرسانی دسته‌بندی {Id} با موفقیت انجام شد.", entity.Id);
    }

    public async Task DeleteAsync(CategoryDto categoryDto)
    {
        await EnsurePermissionAsync(PermissionType.Full, "شما دسترسی لازم برای حذف دسته‌بندی را ندارید.");
        _logger.LogWarning("شروع فرآیند حذف دسته‌بندی با شناسه {Id}.", categoryDto.Id);

        var existingCategory = await _categoryRepo.GetByIdAsync(categoryDto.Id);
        if (existingCategory == null)
            throw new NotFoundException("دسته‌بندی", categoryDto.Id);

        var allCategoryProjects = await _categoryProjectRepo.GetAllAsync();
        var projectsToDelete = allCategoryProjects.Where(cp => cp.CategoryId == categoryDto.Id).ToList();
        if (projectsToDelete.Any())
        {
            await _categoryProjectRepo.DeleteRangeAsync(projectsToDelete);
            _logger.LogInformation("تعداد {Count} پروژه مرتبط با دسته‌بندی {Id} حذف شد.", projectsToDelete.Count, categoryDto.Id);
        }

        await _categoryRepo.DeleteAsync(categoryDto.Id);
        _logger.LogWarning("دسته‌بندی با شناسه {Id} با موفقیت به طور کامل حذف شد.", categoryDto.Id);
    }

    public async Task DeleteRangeAsync(IEnumerable<int> ids)
    {
        await EnsurePermissionAsync(PermissionType.Full, "شما دسترسی لازم برای حذف گروهی دسته‌بندی‌ها را ندارید.");
        var idsList = ids.ToList();
        _logger.LogWarning("درخواست حذف گروهی دسته‌بندی‌ها. تعداد: {Count}", idsList.Count);

        var allCategories = await _categoryRepo.GetAllAsync();
        var toDelete = allCategories.Where(c => idsList.Contains(c.Id)).ToList();

        if (!toDelete.Any())
            throw new NotFoundException("هیچ دسته‌بندی برای حذف یافت نشد.");

        var allCategoryProjects = await _categoryProjectRepo.GetAllAsync();
        var projectsToDelete = allCategoryProjects.Where(cp => idsList.Contains(cp.CategoryId)).ToList();
        if (projectsToDelete.Any())
        {
            await _categoryProjectRepo.DeleteRangeAsync(projectsToDelete);
        }

        await _categoryRepo.DeleteRangeAsync(toDelete);
        _logger.LogWarning("{Count} دسته‌بندی با موفقیت به صورت گروهی حذف شدند.", toDelete.Count);
    }

    public async Task UpdateCategoriesStatusAsync(IEnumerable<int> ids, bool isActive)
    {
        await EnsurePermissionAsync(PermissionType.SystemSection, "شما دسترسی لازم برای تغییر وضعیت دسته‌بندی‌ها را ندارید.");
        var idsList = ids.ToList();
        _logger.LogInformation("درخواست تغییر وضعیت {Count} دسته‌بندی به وضعیت فعال={IsActive}.", idsList.Count, isActive);

        var categories = await _categoryRepo.GetAllAsync();
        var categoriesToUpdate = categories.Where(r => idsList.Contains(r.Id)).ToList();

        if (!categoriesToUpdate.Any())
            throw new NotFoundException("هیچ دسته‌بندی برای تغییر وضعیت یافت نشد.");

        foreach (var category in categoriesToUpdate)
        {
            category.IsActive = isActive;
            await _categoryRepo.UpdateAsync(category);
        }

        _logger.LogInformation("وضعیت دسته‌بندی‌ها با موفقیت تغییر کرد.");
    }

    public async Task<List<CategoryDto>> GetCategoriesByProjectIdAsync(int projectId)
    {
        _logger.LogInformation("دریافت دسته‌بندی‌های فعال برای پروژه با شناسه: {ProjectId}", projectId);
        if (projectId <= 0) return new List<CategoryDto>();

        var categories = await _categoryRepo.GetAllWithIncludesAsync(c => c.CategoryProjects);
        var filtered = categories
            .Where(c => c.IsActive && c.CategoryProjects.Any(cp => cp.ProjectId == projectId))
            .ToList();

        return filtered.Adapt<List<CategoryDto>>();
    }

    public async Task<List<CategoryDto>> GetCategoriesByUserRolesAsync(IEnumerable<string> userRoles)
    {
        _logger.LogInformation("دریافت تمامی دسته‌بندی‌های فعال.");
        var categories = await _categoryRepo.GetAllWithIncludesAsync(c => c.CategoryProjects);
        return categories.Where(c => c.IsActive).Adapt<List<CategoryDto>>();
    }
}