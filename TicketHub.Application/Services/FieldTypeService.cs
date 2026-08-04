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

public class FieldTypeService : IFieldTypeService
{
    private readonly IRepository<FieldType> _repository;
    private readonly ILogger<FieldTypeService> _logger;
    private readonly IValidator<FieldTypeDto> _validator;

    public FieldTypeService(
        IRepository<FieldType> repository,
        ILogger<FieldTypeService> logger,
        IValidator<FieldTypeDto> validator)
    {
        _repository = repository;
        _logger = logger;
        _validator = validator;
    }

    private async Task ValidateDtoAsync(FieldTypeDto dto)
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

    public async Task<IEnumerable<FieldTypeDto>> GetAllAsync()
    {
        _logger.LogInformation("شروع دریافت تمامی نوع فیلدها.");
        var entities = await _repository.GetAllAsync();
        return entities.Adapt<IEnumerable<FieldTypeDto>>();
    }

    public async Task<FieldTypeDto?> GetByIdAsync(int id)
    {
        _logger.LogInformation("جستجوی نوع فیلد با شناسه {Id}.", id);

        var entity = await _repository.GetByIdAsync(id);
        if (entity == null)
            throw new NotFoundException("نوع فیلد", id);

        return entity.Adapt<FieldTypeDto>();
    }

    public async Task AddAsync(FieldTypeDto dto)
    {
        await ValidateDtoAsync(dto);

        _logger.LogInformation("شروع ایجاد نوع فیلد جدید.");
        var entity = dto.Adapt<FieldType>();

        await _repository.AddAsync(entity);
        _logger.LogInformation("نوع فیلد با موفقیت ایجاد شد.");
    }

    public async Task UpdateAsync(FieldTypeDto dto)
    {
        await ValidateDtoAsync(dto);

        _logger.LogInformation("ویرایش نوع فیلد با شناسه {Id}.", dto.Id);

        var existingEntity = await _repository.GetByIdAsync(dto.Id);
        if (existingEntity == null)
            throw new NotFoundException("نوع فیلد", dto.Id);

        var entity = dto.Adapt<FieldType>();
        await _repository.UpdateAsync(entity);

        _logger.LogInformation("نوع فیلد با شناسه {Id} با موفقیت ویرایش شد.", dto.Id);
    }

    public async Task DeleteAsync(int id)
    {
        _logger.LogWarning("درخواست حذف نوع فیلد با شناسه {Id}.", id);

        var existingEntity = await _repository.GetByIdAsync(id);
        if (existingEntity == null)
            throw new NotFoundException("نوع فیلد", id);

        await _repository.DeleteAsync(id);
        _logger.LogInformation("نوع فیلد با شناسه {Id} با موفقیت حذف شد.", id);
    }
}