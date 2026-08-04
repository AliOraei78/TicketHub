using Mapster;
using Microsoft.Extensions.Logging;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;

namespace TicketHub.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly IRepository<Category> _categoryRepo;
    private readonly IRepository<CategoryProject> _categoryProjectRepo;
    private readonly IRepository<CategoryRole> _categoryRoleRepo;
    private readonly IRepository<Role> _roleRepo;
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(
        IRepository<Category> categoryRepo,
        IRepository<CategoryProject> categoryProjectRepo,
        IRepository<CategoryRole> categoryRoleRepo,
        IRepository<Role> roleRepo,
        ILogger<CategoryService> logger)
    {
        _categoryRepo = categoryRepo;
        _categoryProjectRepo = categoryProjectRepo;
        _categoryRoleRepo = categoryRoleRepo;
        _roleRepo = roleRepo;
        _logger = logger;
    }

    public async Task<List<CategoryDto>> GetAllAsync()
    {
        try
        {
            _logger.LogInformation("شروع دریافت تمامی دسته‌بندی‌ها از دیتابیس.");

            var categories = await _categoryRepo.GetAllWithIncludesAsync(
                c => c.CategoryProjects,
                c => c.CategoryRoles);

            _logger.LogInformation("تعداد {Count} دسته‌بندی با موفقیت دریافت شد.", categories.Count());
            return categories.Adapt<List<CategoryDto>>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت تمامی دسته‌بندی‌ها.");
            throw;
        }
    }

    public async Task<CategoryDto?> GetByIdAsync(int id)
    {
        try
        {
            _logger.LogInformation("جستجوی دسته‌بندی با شناسه: {Id}", id);

            var categories = await _categoryRepo.GetAllWithIncludesAsync(
                c => c.CategoryProjects,
                c => c.CategoryRoles);

            var category = categories.FirstOrDefault(c => c.Id == id);

            if (category == null)
                _logger.LogWarning("دسته‌بندی با شناسه {Id} یافت نشد.", id);
            else
                _logger.LogInformation("دسته‌بندی با شناسه {Id} با موفقیت پیدا شد.", id);

            return category?.Adapt<CategoryDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت دسته‌بندی با شناسه {Id}.", id);
            throw;
        }
    }

    public async Task<CategoryDto> AddAsync(CategoryDto dto)
    {
        try
        {
            _logger.LogInformation("شروع فرآیند ایجاد دسته‌بندی جدید.");

            var entity = dto.Adapt<Category>();
            entity.CreatedAt = DateTime.UtcNow;

            await _categoryRepo.AddAsync(entity);
            _logger.LogInformation("دسته‌بندی پایه با شناسه {Id} ایجاد شد.", entity.Id);

            if (dto.ProjectIds?.Any() == true)
            {
                _logger.LogInformation("افزودن {Count} پروژه به دسته‌بندی {Id}.", dto.ProjectIds.Count(), entity.Id);
                foreach (var projectId in dto.ProjectIds)
                {
                    await _categoryProjectRepo.AddAsync(new CategoryProject
                    {
                        CategoryId = entity.Id,
                        ProjectId = projectId
                    });
                }
            }

            if (dto.RoleIds?.Any() == true)
            {
                _logger.LogInformation("افزودن {Count} نقش به دسته‌بندی {Id}.", dto.RoleIds.Count(), entity.Id);
                foreach (var roleId in dto.RoleIds)
                {
                    await _categoryRoleRepo.AddAsync(new CategoryRole
                    {
                        CategoryId = entity.Id,
                        RoleId = roleId
                    });
                }
            }

            _logger.LogInformation("فرآیند ایجاد دسته‌بندی {Id} با موفقیت به پایان رسید.", entity.Id);
            return entity.Adapt<CategoryDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در ایجاد دسته‌بندی جدید.");
            throw;
        }
    }

    public async Task UpdateAsync(CategoryDto dto)
    {
        try
        {
            _logger.LogInformation("شروع فرآیند بروزرسانی دسته‌بندی با شناسه {Id}.", dto.Id);

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
                _logger.LogInformation("ثبت {Count} پروژه جدید برای دسته‌بندی {Id}.", dto.ProjectIds.Count(), entity.Id);
                foreach (var projectId in dto.ProjectIds)
                {
                    await _categoryProjectRepo.AddAsync(new CategoryProject
                    {
                        CategoryId = entity.Id,
                        ProjectId = projectId
                    });
                }
            }

            // نقش‌ها
            var allCategoryRoles = await _categoryRoleRepo.GetAllAsync();
            var oldRoles = allCategoryRoles.Where(cr => cr.CategoryId == entity.Id).ToList();

            if (oldRoles.Any())
            {
                _logger.LogInformation("حذف {Count} نقش قدیمی از دسته‌بندی {Id}.", oldRoles.Count, entity.Id);
                await _categoryRoleRepo.DeleteRangeAsync(oldRoles);
            }

            if (dto.RoleIds?.Any() == true)
            {
                _logger.LogInformation("ثبت {Count} نقش جدید برای دسته‌بندی {Id}.", dto.RoleIds.Count(), entity.Id);
                foreach (var roleId in dto.RoleIds)
                {
                    await _categoryRoleRepo.AddAsync(new CategoryRole
                    {
                        CategoryId = entity.Id,
                        RoleId = roleId
                    });
                }
            }

            _logger.LogInformation("بروزرسانی دسته‌بندی {Id} با موفقیت انجام شد.", entity.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در بروزرسانی دسته‌بندی با شناسه {Id}.", dto.Id);
            throw;
        }
    }

    public async Task DeleteAsync(CategoryDto categoryDto)
    {
        try
        {
            _logger.LogWarning("شروع فرآیند حذف دسته‌بندی با شناسه {Id}.", categoryDto.Id);

            var allCategoryProjects = await _categoryProjectRepo.GetAllAsync();
            var projectsToDelete = allCategoryProjects.Where(cp => cp.CategoryId == categoryDto.Id).ToList();
            if (projectsToDelete.Any())
            {
                await _categoryProjectRepo.DeleteRangeAsync(projectsToDelete);
                _logger.LogInformation("تعداد {Count} پروژه مرتبط با دسته‌بندی {Id} حذف شد.", projectsToDelete.Count, categoryDto.Id);
            }

            var allCategoryRoles = await _categoryRoleRepo.GetAllAsync();
            var rolesToDelete = allCategoryRoles.Where(cr => cr.CategoryId == categoryDto.Id).ToList();
            if (rolesToDelete.Any())
            {
                await _categoryRoleRepo.DeleteRangeAsync(rolesToDelete);
                _logger.LogInformation("تعداد {Count} نقش مرتبط با دسته‌بندی {Id} حذف شد.", rolesToDelete.Count, categoryDto.Id);
            }

            await _categoryRepo.DeleteAsync(categoryDto.Id);
            _logger.LogWarning("دسته‌بندی با شناسه {Id} با موفقیت به طور کامل حذف شد.", categoryDto.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در حذف دسته‌بندی با شناسه {Id}.", categoryDto.Id);
            throw;
        }
    }

    public async Task DeleteRangeAsync(IEnumerable<int> ids)
    {
        try
        {
            var idsList = ids.ToList();
            _logger.LogWarning("درخواست حذف گروهی دسته‌بندی‌ها. تعداد: {Count}", idsList.Count);

            var allCategoryProjects = await _categoryProjectRepo.GetAllAsync();
            var projectsToDelete = allCategoryProjects.Where(cp => idsList.Contains(cp.CategoryId)).ToList();
            if (projectsToDelete.Any())
            {
                await _categoryProjectRepo.DeleteRangeAsync(projectsToDelete);
            }

            var allCategoryRoles = await _categoryRoleRepo.GetAllAsync();
            var rolesToDelete = allCategoryRoles.Where(cr => idsList.Contains(cr.CategoryId)).ToList();
            if (rolesToDelete.Any())
            {
                await _categoryRoleRepo.DeleteRangeAsync(rolesToDelete);
            }

            var allCategories = await _categoryRepo.GetAllAsync();
            var toDelete = allCategories.Where(c => idsList.Contains(c.Id)).ToList();

            if (toDelete.Any())
            {
                await _categoryRepo.DeleteRangeAsync(toDelete);
                _logger.LogWarning("{Count} دسته‌بندی با موفقیت به صورت گروهی حذف شدند.", toDelete.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در حذف گروهی دسته‌بندی‌ها.");
            throw;
        }
    }

    public async Task UpdateCategoriesStatusAsync(IEnumerable<int> ids, bool isActive)
    {
        try
        {
            var idsList = ids.ToList();
            _logger.LogInformation("درخواست تغییر وضعیت {Count} دسته‌بندی به وضعیت فعال={IsActive}.", idsList.Count, isActive);

            var categories = await _categoryRepo.GetAllAsync();
            var categoriesToUpdate = categories.Where(r => idsList.Contains(r.Id)).ToList();

            foreach (var category in categoriesToUpdate)
            {
                category.IsActive = isActive;
                await _categoryRepo.UpdateAsync(category);
            }

            _logger.LogInformation("وضعیت دسته‌بندی‌ها با موفقیت تغییر کرد.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در تغییر وضعیت گروهی دسته‌بندی‌ها.");
            throw;
        }
    }

    public async Task<List<CategoryDto>> GetCategoriesByUserRolesAsync(IEnumerable<string> userRoles)
    {
        try
        {
            var rolesList = userRoles.ToList();
            _logger.LogInformation("جستجوی دسته‌بندی‌ها بر اساس {Count} نقش کاربر.", rolesList.Count);

            var allRoles = await _roleRepo.GetAllAsync();
            var userRoleIds = allRoles
                .Where(r => rolesList.Contains(r.Name))
                .Select(r => r.Id)
                .ToList();

            var categories = await _categoryRepo.GetAllWithIncludesAsync(c => c.CategoryRoles);

            var filteredCategories = categories
                .Where(c => c.CategoryRoles.Any(cr => userRoleIds.Contains(cr.RoleId)))
                .ToList();

            _logger.LogInformation("تعداد {Count} دسته‌بندی منطبق با نقش‌های کاربر یافت شد.", filteredCategories.Count);

            return filteredCategories.Adapt<List<CategoryDto>>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در جستجوی دسته‌بندی‌ها بر اساس نقش کاربر.");
            throw;
        }
    }
}