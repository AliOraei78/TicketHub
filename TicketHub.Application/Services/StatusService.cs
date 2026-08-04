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

public class StatusService : IStatusService
{
    private readonly IRepository<Status> _repository;
    private readonly ILogger<StatusService> _logger;
    private readonly IValidator<StatusDto> _validator;

    public StatusService(
        IRepository<Status> repository,
        ILogger<StatusService> logger,
        IValidator<StatusDto> validator)
    {
        _repository = repository;
        _logger = logger;
        _validator = validator;
    }

    private async Task ValidateDtoAsync(StatusDto dto)
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

    public async Task<List<StatusDto>> GetAllAsync()
    {
        _logger.LogInformation("شروع دریافت تمامی وضعیت‌ها.");
        var entities = await _repository.GetAllAsync();
        return entities.Adapt<List<StatusDto>>();
    }

    public async Task AddAsync(StatusDto dto)
    {
        await ValidateDtoAsync(dto);

        _logger.LogInformation("شروع ایجاد وضعیت جدید.");
        await _repository.AddAsync(dto.Adapt<Status>());
        _logger.LogInformation("وضعیت با موفقیت ایجاد شد.");
    }

    public async Task UpdateAsync(StatusDto dto)
    {
        await ValidateDtoAsync(dto);

        _logger.LogInformation("ویرایش وضعیت با شناسه {Id}.", dto.Id);

        var existingEntity = await _repository.GetByIdAsync(dto.Id);
        if (existingEntity == null)
            throw new NotFoundException("وضعیت", dto.Id);

        await _repository.UpdateAsync(dto.Adapt<Status>());
        _logger.LogInformation("وضعیت با شناسه {Id} با موفقیت ویرایش شد.", dto.Id);
    }

    public async Task DeleteAsync(int id)
    {
        _logger.LogWarning("درخواست حذف وضعیت با شناسه {Id}.", id);

        var existingEntity = await _repository.GetByIdAsync(id);
        if (existingEntity == null)
            throw new NotFoundException("وضعیت", id);

        await _repository.DeleteAsync(id); // حذف قطعی
        _logger.LogInformation("وضعیت با شناسه {Id} با موفقیت حذف شد.", id);
    }

    public async Task DeleteRangeAsync(IEnumerable<int> ids)
    {
        var idsList = ids.ToList();
        _logger.LogWarning("درخواست حذف گروهی وضعیت‌ها به تعداد {Count}.", idsList.Count);

        var allStatuses = await _repository.GetAllAsync();
        var statusesToDelete = allStatuses.Where(s => idsList.Contains(s.Id)).ToList();

        if (!statusesToDelete.Any())
            throw new NotFoundException("هیچ وضعیتی برای حذف یافت نشد.");

        await _repository.DeleteRangeAsync(statusesToDelete); // حذف قطعی گروهی
        _logger.LogInformation("تعداد {Count} وضعیت با موفقیت حذف شدند.", statusesToDelete.Count);
    }

    public async Task UpdateStatesStatusAsync(IEnumerable<int> ids, bool isActive)
    {
        var idsList = ids.ToList();
        _logger.LogInformation("درخواست تغییر وضعیت {Count} وضعیت به وضعیت فعال={IsActive}.", idsList.Count, isActive);

        var statuses = await _repository.GetAllAsync();
        var statusesToUpdate = statuses.Where(s => idsList.Contains(s.Id)).ToList();

        if (!statusesToUpdate.Any())
            throw new NotFoundException("هیچ وضعیتی برای تغییر وضعیت یافت نشد.");

        foreach (var status in statusesToUpdate)
        {
            status.IsActive = isActive;
            await _repository.UpdateAsync(status);
        }

        _logger.LogInformation("وضعیت‌ها با موفقیت تغییر کردند.");
    }
}