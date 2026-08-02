using Mapster;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;

namespace TicketHub.Application.Services;

public class TicketFieldService : ITicketFieldService
{
    private readonly IRepository<TicketField> _ticketFieldRepo;
    private readonly IRepository<FieldCategory> _fieldCategoryRepo;

    public TicketFieldService(
        IRepository<TicketField> ticketFieldRepo,
        IRepository<FieldCategory> fieldCategoryRepo)
    {
        _ticketFieldRepo = ticketFieldRepo;
        _fieldCategoryRepo = fieldCategoryRepo;
    }

    public async Task<List<TicketFieldDto>> GetAllAsync()
    {
        var fields = await _ticketFieldRepo.GetAllWithIncludesAsync(
            f => f.FieldCategories,
            f => f.FieldType);

        return fields.Adapt<List<TicketFieldDto>>();
    }

    public async Task<TicketFieldDto?> GetByIdAsync(int id)
    {
        var fields = await _ticketFieldRepo.GetAllWithIncludesAsync(
            f => f.FieldCategories,
            f => f.FieldType);

        var field = fields.FirstOrDefault(f => f.Id == id);
        return field?.Adapt<TicketFieldDto>();
    }

    public async Task<TicketFieldDto> AddAsync(TicketFieldDto dto)
    {
        var entity = dto.Adapt<TicketField>();
        entity.CreatedAt = DateTime.UtcNow;

        await _ticketFieldRepo.AddAsync(entity);

        // ذخیره دسته‌بندی‌ها (Categories)
        if (dto.CategoryIds?.Any() == true)
        {
            foreach (var categoryId in dto.CategoryIds)
            {
                await _fieldCategoryRepo.AddAsync(new FieldCategory
                {
                    TicketFieldId = entity.Id,
                    CategoryId = categoryId
                });
            }
        }

        return entity.Adapt<TicketFieldDto>();
    }

    public async Task UpdateAsync(TicketFieldDto dto)
    {
        var entity = dto.Adapt<TicketField>();
        await _ticketFieldRepo.UpdateAsync(entity);

        // بروزرسانی دسته‌بندی‌ها: حذف قبلی‌ها و ثبت جدیدها
        var allFieldCategories = await _fieldCategoryRepo.GetAllAsync();
        var oldCategories = allFieldCategories.Where(fc => fc.TicketFieldId == entity.Id).ToList();

        if (oldCategories.Any())
        {
            await _fieldCategoryRepo.DeleteRangeAsync(oldCategories);
        }

        if (dto.CategoryIds?.Any() == true)
        {
            foreach (var categoryId in dto.CategoryIds)
            {
                await _fieldCategoryRepo.AddAsync(new FieldCategory
                {
                    TicketFieldId = entity.Id,
                    CategoryId = categoryId
                });
            }
        }
    }

    public async Task DeleteAsync(TicketFieldDto dto)
    {
        // حذف وابستگی‌های دسته‌بندی
        var allFieldCategories = await _fieldCategoryRepo.GetAllAsync();
        var categoriesToDelete = allFieldCategories.Where(fc => fc.TicketFieldId == dto.Id).ToList();
        if (categoriesToDelete.Any())
        {
            await _fieldCategoryRepo.DeleteRangeAsync(categoriesToDelete);
        }

        // حذف خود فیلد
        await _ticketFieldRepo.DeleteAsync(dto.Id);
    }

    public async Task DeleteRangeAsync(IEnumerable<int> ids)
    {
        // حذف وابستگی‌های دسته‌بندی مرتبط
        var allFieldCategories = await _fieldCategoryRepo.GetAllAsync();
        var categoriesToDelete = allFieldCategories.Where(fc => ids.Contains(fc.TicketFieldId)).ToList();
        if (categoriesToDelete.Any())
        {
            await _fieldCategoryRepo.DeleteRangeAsync(categoriesToDelete);
        }

        // حذف خود فیلدها
        var allFields = await _ticketFieldRepo.GetAllAsync();
        var toDelete = allFields.Where(f => ids.Contains(f.Id)).ToList();

        if (toDelete.Any())
        {
            await _ticketFieldRepo.DeleteRangeAsync(toDelete);
        }
    }

    public async Task UpdateTicketFieldsStatusAsync(IEnumerable<int> ids, bool isActive)
    {
        var fields = await _ticketFieldRepo.GetAllAsync();
        var fieldsToUpdate = fields.Where(f => ids.Contains(f.Id)).ToList();

        foreach (var field in fieldsToUpdate)
        {
            field.IsActive = isActive;
            await _ticketFieldRepo.UpdateAsync(field);
        }
    }
}
