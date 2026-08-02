using Mapster;
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

    public CategoryService(
        IRepository<Category> categoryRepo,
        IRepository<CategoryProject> categoryProjectRepo,
        IRepository<CategoryRole> categoryRoleRepo)
    {
        _categoryRepo = categoryRepo;
        _categoryProjectRepo = categoryProjectRepo;
        _categoryRoleRepo = categoryRoleRepo;
    }

    public async Task<List<CategoryDto>> GetAllAsync()
    {
        var categories = await _categoryRepo.GetAllWithIncludesAsync(
            c => c.CategoryProjects,
            c => c.CategoryRoles);

        return categories.Adapt<List<CategoryDto>>();
    }

    public async Task<CategoryDto?> GetByIdAsync(int id)
    {
        var categories = await _categoryRepo.GetAllWithIncludesAsync(
            c => c.CategoryProjects,
            c => c.CategoryRoles);

        var category = categories.FirstOrDefault(c => c.Id == id);
        return category?.Adapt<CategoryDto>();
    }

    public async Task<CategoryDto> AddAsync(CategoryDto dto)
    {
        var entity = dto.Adapt<Category>();
        entity.CreatedAt = DateTime.UtcNow;

        await _categoryRepo.AddAsync(entity);

        // ذخیره پروژه‌ها
        if (dto.ProjectIds?.Any() == true)
        {
            foreach (var projectId in dto.ProjectIds)
            {
                await _categoryProjectRepo.AddAsync(new CategoryProject
                {
                    CategoryId = entity.Id,
                    ProjectId = projectId
                });
            }
        }

        // ذخیره نقش‌ها
        if (dto.RoleIds?.Any() == true)
        {
            foreach (var roleId in dto.RoleIds)
            {
                await _categoryRoleRepo.AddAsync(new CategoryRole
                {
                    CategoryId = entity.Id,
                    RoleId = roleId
                });
            }
        }

        return entity.Adapt<CategoryDto>();
    }

    public async Task UpdateAsync(CategoryDto dto)
    {
        var entity = dto.Adapt<Category>();
        await _categoryRepo.UpdateAsync(entity);

        // بروزرسانی پروژه‌ها: حذف قبلی‌ها و ثبت جدیدها
        var allCategoryProjects = await _categoryProjectRepo.GetAllAsync();
        var oldProjects = allCategoryProjects.Where(cp => cp.CategoryId == entity.Id).ToList();

        if (oldProjects.Any())
        {
            await _categoryProjectRepo.DeleteRangeAsync(oldProjects);
        }

        if (dto.ProjectIds?.Any() == true)
        {
            foreach (var projectId in dto.ProjectIds)
            {
                await _categoryProjectRepo.AddAsync(new CategoryProject
                {
                    CategoryId = entity.Id,
                    ProjectId = projectId
                });
            }
        }

        // بروزرسانی نقش‌ها: حذف قبلی‌ها و ثبت جدیدها
        var allCategoryRoles = await _categoryRoleRepo.GetAllAsync();
        var oldRoles = allCategoryRoles.Where(cr => cr.CategoryId == entity.Id).ToList();

        if (oldRoles.Any())
        {
            await _categoryRoleRepo.DeleteRangeAsync(oldRoles);
        }

        if (dto.RoleIds?.Any() == true)
        {
            foreach (var roleId in dto.RoleIds)
            {
                await _categoryRoleRepo.AddAsync(new CategoryRole
                {
                    CategoryId = entity.Id,
                    RoleId = roleId
                });
            }
        }
    }

    public async Task DeleteAsync(CategoryDto categoryDto) =>
        await _categoryRepo.DeleteAsync(categoryDto.Id);

    public async Task DeleteRangeAsync(IEnumerable<int> ids)
    {
        var allCategories = await _categoryRepo.GetAllAsync();
        var toDelete = allCategories.Where(c => ids.Contains(c.Id)).ToList();

        if (toDelete.Any())
        {
            await _categoryRepo.DeleteRangeAsync(toDelete);
        }
    }
}