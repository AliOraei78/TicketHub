using Mapster;
using Microsoft.Extensions.Logging;
using FluentValidation;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Core.Common.Exceptions;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;
using ValidationException = TicketHub.Core.Common.Exceptions.ValidationException;

namespace TicketHub.Application.Services;

public class TicketFieldService : ITicketFieldService
{
    private readonly IRepository<TicketField> _ticketFieldRepo;
    private readonly IRepository<FieldCategory> _fieldCategoryRepo;
    private readonly ILogger<TicketFieldService> _logger;
    private readonly IValidator<TicketFieldDto> _validator;

    public TicketFieldService(
        IRepository<TicketField> ticketFieldRepo,
        IRepository<FieldCategory> fieldCategoryRepo,
        ILogger<TicketFieldService> logger,
        IValidator<TicketFieldDto> validator)
    {
        _ticketFieldRepo = ticketFieldRepo;
        _fieldCategoryRepo = fieldCategoryRepo;
        _logger = logger;
        _validator = validator;
    }

    private async Task ValidateDtoAsync(TicketFieldDto dto)
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

    public async Task<List<TicketFieldDto>> GetAllAsync()
    {
        _logger.LogInformation("شروع دریافت لیست تمامی فیلدهای تیکت.");

        var fields = await _ticketFieldRepo.GetAllWithIncludesAsync(
            f => f.FieldCategories,
            f => f.FieldType);

        return fields.Adapt<List<TicketFieldDto>>();
    }

    public async Task<TicketFieldDto?> GetByIdAsync(int id)
    {
        _logger.LogInformation("جستجوی فیلد تیکت با شناسه {Id}.", id);

        var fields = await _ticketFieldRepo.GetAllWithIncludesAsync(
            f => f.FieldCategories,
            f => f.FieldType);

        var field = fields.FirstOrDefault(f => f.Id == id);

        if (field == null)
            throw new NotFoundException("فیلد تیکت", id);

        return field.Adapt<TicketFieldDto>();
    }

    public async Task<TicketFieldDto> AddAsync(TicketFieldDto dto)
    {
        await ValidateDtoAsync(dto);

        _logger.LogInformation("شروع ایجاد فیلد تیکت جدید.");

        var entity = dto.Adapt<TicketField>();
        entity.CreatedAt = DateTime.UtcNow;

        await _ticketFieldRepo.AddAsync(entity);
        _logger.LogInformation("فیلد تیکت پایه با شناسه {Id} ایجاد شد.", entity.Id);

        if (dto.CategoryIds?.Any() == true)
        {
            _logger.LogInformation("افزودن {Count} دسته‌بندی به فیلد تیکت {Id}.", dto.CategoryIds.Count, entity.Id);

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

    public async Task UpdateAsync(TicketFieldDto dto)
    {
        await ValidateDtoAsync(dto);

        _logger.LogInformation("شروع ویرایش فیلد تیکت با شناسه {Id}.", dto.Id);

        var existingField = await _ticketFieldRepo.GetByIdAsync(dto.Id);
        if (existingField == null)
            throw new NotFoundException("فیلد تیکت", dto.Id);

        var entity = dto.Adapt<TicketField>();
        await _ticketFieldRepo.UpdateAsync(entity);

        var allFieldCategories = await _fieldCategoryRepo.GetAllAsync();
        var oldCategories = allFieldCategories.Where(fc => fc.TicketFieldId == entity.Id).ToList();

        if (oldCategories.Any())
        {
            _logger.LogInformation("حذف {Count} ارتباط دسته‌بندی قدیمی از فیلد تیکت {Id}.", oldCategories.Count, entity.Id);
            await _fieldCategoryRepo.DeleteRangeAsync(oldCategories);
        }

        if (dto.CategoryIds?.Any() == true)
        {
            _logger.LogInformation("ثبت {Count} ارتباط دسته‌بندی جدید برای فیلد تیکت {Id}.", dto.CategoryIds.Count, entity.Id);

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

    public async Task DeleteAsync(TicketFieldDto dto)
    {
        _logger.LogWarning("درخواست حذف فیلد تیکت با شناسه {Id}.", dto.Id);

        var existingField = await _ticketFieldRepo.GetByIdAsync(dto.Id);
        if (existingField == null)
            throw new NotFoundException("فیلد تیکت", dto.Id);

        var allFieldCategories = await _fieldCategoryRepo.GetAllAsync();
        var categoriesToDelete = allFieldCategories.Where(fc => fc.TicketFieldId == dto.Id).ToList();

        if (categoriesToDelete.Any())
        {
            _logger.LogInformation("حذف {Count} وابستگی دسته‌بندی مرتبط با فیلد تیکت {Id}.", categoriesToDelete.Count, dto.Id);
            await _fieldCategoryRepo.DeleteRangeAsync(categoriesToDelete);
        }

        await _ticketFieldRepo.DeleteAsync(dto.Id); // حذف قطعی موجودیت اصلی
        _logger.LogInformation("فیلد تیکت با شناسه {Id} با موفقیت حذف شد.", dto.Id);
    }

    public async Task DeleteRangeAsync(IEnumerable<int> ids)
    {
        var idsList = ids.ToList();
        _logger.LogWarning("درخواست حذف گروهی فیلدهای تیکت به تعداد {Count}.", idsList.Count);

        var allFields = await _ticketFieldRepo.GetAllAsync();
        var toDelete = allFields.Where(f => idsList.Contains(f.Id)).ToList();

        if (!toDelete.Any())
            throw new NotFoundException("هیچ فیلد تیکتی برای حذف یافت نشد.");

        var allFieldCategories = await _fieldCategoryRepo.GetAllAsync();
        var categoriesToDelete = allFieldCategories.Where(fc => idsList.Contains(fc.TicketFieldId)).ToList();

        if (categoriesToDelete.Any())
        {
            await _fieldCategoryRepo.DeleteRangeAsync(categoriesToDelete);
        }

        await _ticketFieldRepo.DeleteRangeAsync(toDelete);
        _logger.LogInformation("تعداد {Count} فیلد تیکت با موفقیت حذف شدند.", toDelete.Count);
    }

    public async Task UpdateTicketFieldsStatusAsync(IEnumerable<int> ids, bool isActive)
    {
        var idsList = ids.ToList();
        _logger.LogInformation("درخواست تغییر وضعیت {Count} فیلد تیکت به وضعیت فعال={IsActive}.", idsList.Count, isActive);

        var fields = await _ticketFieldRepo.GetAllAsync();
        var fieldsToUpdate = fields.Where(f => idsList.Contains(f.Id)).ToList();

        if (!fieldsToUpdate.Any())
            throw new NotFoundException("هیچ فیلد تیکتی برای تغییر وضعیت یافت نشد.");

        foreach (var field in fieldsToUpdate)
        {
            field.IsActive = isActive;
            await _ticketFieldRepo.UpdateAsync(field);
        }

        _logger.LogInformation("وضعیت فیلدهای تیکت با موفقیت تغییر کرد.");
    }

    public async Task<List<TicketFieldDto>> GetFieldsByCategoryIdAsync(int categoryId)
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
}