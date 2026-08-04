using Mapster;
using Microsoft.Extensions.Logging;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;

namespace TicketHub.Application.Services;

public class TicketFieldService : ITicketFieldService
{
    private readonly IRepository<TicketField> _ticketFieldRepo;
    private readonly IRepository<FieldCategory> _fieldCategoryRepo;
    private readonly ILogger<TicketFieldService> _logger;

    public TicketFieldService(
        IRepository<TicketField> ticketFieldRepo,
        IRepository<FieldCategory> fieldCategoryRepo,
        ILogger<TicketFieldService> logger)
    {
        _ticketFieldRepo = ticketFieldRepo;
        _fieldCategoryRepo = fieldCategoryRepo;
        _logger = logger;
    }

    public async Task<List<TicketFieldDto>> GetAllAsync()
    {
        try
        {
            _logger.LogInformation("شروع دریافت لیست تمامی فیلدهای تیکت.");

            var fields = await _ticketFieldRepo.GetAllWithIncludesAsync(
                f => f.FieldCategories,
                f => f.FieldType);

            return fields.Adapt<List<TicketFieldDto>>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت لیست فیلدهای تیکت.");
            throw;
        }
    }

    public async Task<TicketFieldDto?> GetByIdAsync(int id)
    {
        try
        {
            _logger.LogInformation("جستجوی فیلد تیکت با شناسه {Id}.", id);

            var fields = await _ticketFieldRepo.GetAllWithIncludesAsync(
                f => f.FieldCategories,
                f => f.FieldType);

            var field = fields.FirstOrDefault(f => f.Id == id);

            if (field == null)
                _logger.LogWarning("فیلد تیکت با شناسه {Id} یافت نشد.", id);

            return field?.Adapt<TicketFieldDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت فیلد تیکت با شناسه {Id}.", id);
            throw;
        }
    }

    public async Task<TicketFieldDto> AddAsync(TicketFieldDto dto)
    {
        try
        {
            _logger.LogInformation("شروع ایجاد فیلد تیکت جدید.");

            var entity = dto.Adapt<TicketField>();
            entity.CreatedAt = DateTime.UtcNow;

            await _ticketFieldRepo.AddAsync(entity);
            _logger.LogInformation("فیلد تیکت پایه با شناسه {Id} ایجاد شد.", entity.Id);

            // ذخیره دسته‌بندی‌ها (Categories)
            if (dto.CategoryIds?.Any() == true)
            {
                _logger.LogInformation("افزودن {Count} دسته‌بندی به فیلد تیکت {Id}.", dto.CategoryIds.Count(), entity.Id);

                foreach (var categoryId in dto.CategoryIds)
                {
                    await _fieldCategoryRepo.AddAsync(new FieldCategory
                    {
                        TicketFieldId = entity.Id,
                        CategoryId = categoryId
                    });
                }
            }

            _logger.LogInformation("فیلد تیکت جدید با موفقیت ایجاد شد.");
            return entity.Adapt<TicketFieldDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در ایجاد فیلد تیکت جدید.");
            throw;
        }
    }

    public async Task UpdateAsync(TicketFieldDto dto)
    {
        try
        {
            _logger.LogInformation("شروع ویرایش فیلد تیکت با شناسه {Id}.", dto.Id);

            var entity = dto.Adapt<TicketField>();
            await _ticketFieldRepo.UpdateAsync(entity);

            // بروزرسانی دسته‌بندی‌ها: حذف قبلی‌ها و ثبت جدیدها
            var allFieldCategories = await _fieldCategoryRepo.GetAllAsync();
            var oldCategories = allFieldCategories.Where(fc => fc.TicketFieldId == entity.Id).ToList();

            if (oldCategories.Any())
            {
                _logger.LogInformation("حذف {Count} ارتباط دسته‌بندی قدیمی از فیلد تیکت {Id}.", oldCategories.Count, entity.Id);
                await _fieldCategoryRepo.DeleteRangeAsync(oldCategories);
            }

            if (dto.CategoryIds?.Any() == true)
            {
                _logger.LogInformation("ثبت {Count} ارتباط دسته‌بندی جدید برای فیلد تیکت {Id}.", dto.CategoryIds.Count(), entity.Id);

                foreach (var categoryId in dto.CategoryIds)
                {
                    await _fieldCategoryRepo.AddAsync(new FieldCategory
                    {
                        TicketFieldId = entity.Id,
                        CategoryId = categoryId
                    });
                }
            }

            _logger.LogInformation("فیلد تیکت با شناسه {Id} با موفقیت ویرایش شد.", dto.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در ویرایش فیلد تیکت با شناسه {Id}.", dto.Id);
            throw;
        }
    }

    public async Task DeleteAsync(TicketFieldDto dto)
    {
        try
        {
            _logger.LogWarning("درخواست حذف فیلد تیکت با شناسه {Id}.", dto.Id);

            // حذف وابستگی‌های دسته‌بندی
            var allFieldCategories = await _fieldCategoryRepo.GetAllAsync();
            var categoriesToDelete = allFieldCategories.Where(fc => fc.TicketFieldId == dto.Id).ToList();

            if (categoriesToDelete.Any())
            {
                _logger.LogInformation("حذف {Count} وابستگی دسته‌بندی مرتبط با فیلد تیکت {Id}.", categoriesToDelete.Count, dto.Id);
                await _fieldCategoryRepo.DeleteRangeAsync(categoriesToDelete);
            }

            // حذف خود فیلد
            await _ticketFieldRepo.DeleteAsync(dto.Id);
            _logger.LogInformation("فیلد تیکت با شناسه {Id} با موفقیت حذف شد.", dto.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در حذف فیلد تیکت با شناسه {Id}.", dto.Id);
            throw;
        }
    }

    public async Task DeleteRangeAsync(IEnumerable<int> ids)
    {
        try
        {
            var idsList = ids.ToList();
            _logger.LogWarning("درخواست حذف گروهی فیلدهای تیکت به تعداد {Count}.", idsList.Count);

            // حذف وابستگی‌های دسته‌بندی مرتبط
            var allFieldCategories = await _fieldCategoryRepo.GetAllAsync();
            var categoriesToDelete = allFieldCategories.Where(fc => idsList.Contains(fc.TicketFieldId)).ToList();

            if (categoriesToDelete.Any())
            {
                await _fieldCategoryRepo.DeleteRangeAsync(categoriesToDelete);
            }

            // حذف خود فیلدها
            var allFields = await _ticketFieldRepo.GetAllAsync();
            var toDelete = allFields.Where(f => idsList.Contains(f.Id)).ToList();

            if (toDelete.Any())
            {
                await _ticketFieldRepo.DeleteRangeAsync(toDelete);
                _logger.LogInformation("تعداد {Count} فیلد تیکت با موفقیت حذف شدند.", toDelete.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در حذف گروهی فیلدهای تیکت.");
            throw;
        }
    }

    public async Task UpdateTicketFieldsStatusAsync(IEnumerable<int> ids, bool isActive)
    {
        try
        {
            var idsList = ids.ToList();
            _logger.LogInformation("درخواست تغییر وضعیت {Count} فیلد تیکت به وضعیت فعال={IsActive}.", idsList.Count, isActive);

            var fields = await _ticketFieldRepo.GetAllAsync();
            var fieldsToUpdate = fields.Where(f => idsList.Contains(f.Id)).ToList();

            foreach (var field in fieldsToUpdate)
            {
                field.IsActive = isActive;
                await _ticketFieldRepo.UpdateAsync(field);
            }

            _logger.LogInformation("وضعیت فیلدهای تیکت با موفقیت تغییر کرد.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در تغییر وضعیت گروهی فیلدهای تیکت.");
            throw;
        }
    }

    public async Task<List<TicketFieldDto>> GetFieldsByCategoryIdAsync(int categoryId)
    {
        try
        {
            _logger.LogInformation("جستجوی فیلدهای تیکت مرتبط با دسته‌بندی شناسه {CategoryId}.", categoryId);

            var fields = await _ticketFieldRepo.GetAllWithIncludesAsync(
                f => f.FieldCategories,
                f => f.FieldType);

            var categoryFields = fields
                .Where(f => f.IsActive && f.FieldCategories.Any(fc => fc.CategoryId == categoryId))
                .OrderBy(f => f.SortOrder)
                .ToList();

            _logger.LogInformation("تعداد {Count} فیلد تیکت فعال برای دسته‌بندی {CategoryId} یافت شد.", categoryFields.Count, categoryId);
            return categoryFields.Adapt<List<TicketFieldDto>>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت فیلدهای تیکت بر اساس شناسه دسته‌بندی {CategoryId}.", categoryId);
            throw;
        }
    }
}