using Mapster;
using Microsoft.Extensions.Logging;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;

namespace TicketHub.Application.Services;

public class FieldTypeService : IFieldTypeService
{
    private readonly IRepository<FieldType> _repository;
    private readonly ILogger<FieldTypeService> _logger;

    public FieldTypeService(
        IRepository<FieldType> repository,
        ILogger<FieldTypeService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IEnumerable<FieldTypeDto>> GetAllAsync()
    {
        try
        {
            _logger.LogInformation("شروع دریافت تمامی نوع فیلدها.");
            var entities = await _repository.GetAllAsync();
            return entities.Adapt<IEnumerable<FieldTypeDto>>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت تمامی نوع فیلدها.");
            throw;
        }
    }

    public async Task<FieldTypeDto?> GetByIdAsync(int id)
    {
        try
        {
            _logger.LogInformation("جستجوی نوع فیلد با شناسه {Id}.", id);
            var entity = await _repository.GetByIdAsync(id);
            return entity?.Adapt<FieldTypeDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت نوع فیلد با شناسه {Id}.", id);
            throw;
        }
    }

    public async Task AddAsync(FieldTypeDto dto)
    {
        try
        {
            _logger.LogInformation("شروع ایجاد نوع فیلد جدید.");
            var entity = dto.Adapt<FieldType>();
            await _repository.AddAsync(entity);
            _logger.LogInformation("نوع فیلد با موفقیت ایجاد شد.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در ایجاد نوع فیلد جدید.");
            throw;
        }
    }

    public async Task UpdateAsync(FieldTypeDto dto)
    {
        try
        {
            _logger.LogInformation("ویرایش نوع فیلد با شناسه {Id}.", dto.Id);
            var entity = dto.Adapt<FieldType>();
            await _repository.UpdateAsync(entity);
            _logger.LogInformation("نوع فیلد با شناسه {Id} با موفقیت ویرایش شد.", dto.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در ویرایش نوع فیلد با شناسه {Id}.", dto.Id);
            throw;
        }
    }

    public async Task DeleteAsync(int id)
    {
        try
        {
            _logger.LogWarning("درخواست حذف نوع فیلد با شناسه {Id}.", id);
            await _repository.DeleteAsync(id);
            _logger.LogInformation("نوع فیلد با شناسه {Id} با موفقیت حذف شد.", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در حذف نوع فیلد با شناسه {Id}.", id);
            throw;
        }
    }
}