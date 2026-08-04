using Mapster;
using Microsoft.Extensions.Logging;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;

namespace TicketHub.Application.Services;

public class StatusService : IStatusService
{
    private readonly IRepository<Status> _repository;
    private readonly ILogger<StatusService> _logger;

    public StatusService(
        IRepository<Status> repository,
        ILogger<StatusService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<List<StatusDto>> GetAllAsync()
    {
        try
        {
            _logger.LogInformation("شروع دریافت تمامی وضعیت‌ها.");
            var entities = await _repository.GetAllAsync();
            return entities.Adapt<List<StatusDto>>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت تمامی وضعیت‌ها.");
            throw;
        }
    }

    public async Task AddAsync(StatusDto dto)
    {
        try
        {
            _logger.LogInformation("شروع ایجاد وضعیت جدید.");
            await _repository.AddAsync(dto.Adapt<Status>());
            _logger.LogInformation("وضعیت با موفقیت ایجاد شد.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در ایجاد وضعیت جدید.");
            throw;
        }
    }

    public async Task UpdateAsync(StatusDto dto)
    {
        try
        {
            _logger.LogInformation("ویرایش وضعیت با شناسه {Id}.", dto.Id);
            await _repository.UpdateAsync(dto.Adapt<Status>());
            _logger.LogInformation("وضعیت با شناسه {Id} با موفقیت ویرایش شد.", dto.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در ویرایش وضعیت با شناسه {Id}.", dto.Id);
            throw;
        }
    }

    public async Task DeleteAsync(int id)
    {
        try
        {
            _logger.LogWarning("درخواست حذف وضعیت با شناسه {Id}.", id);
            await _repository.DeleteAsync(id);
            _logger.LogInformation("وضعیت با شناسه {Id} با موفقیت حذف شد.", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در حذف وضعیت با شناسه {Id}.", id);
            throw;
        }
    }

    public async Task DeleteRangeAsync(IEnumerable<int> ids)
    {
        try
        {
            var idsList = ids.ToList();
            _logger.LogWarning("درخواست حذف گروهی وضعیت‌ها به تعداد {Count}.", idsList.Count);

            var entities = new List<Status>();
            foreach (var id in idsList)
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity != null) entities.Add(entity);
            }

            if (entities.Any())
            {
                await _repository.DeleteRangeAsync(entities);
                _logger.LogInformation("تعداد {Count} وضعیت با موفقیت حذف شدند.", entities.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در حذف گروهی وضعیت‌ها.");
            throw;
        }
    }

    public async Task UpdateStatesStatusAsync(IEnumerable<int> ids, bool isActive)
    {
        try
        {
            var idsList = ids.ToList();
            _logger.LogInformation("درخواست تغییر وضعیت {Count} وضعیت به وضعیت فعال={IsActive}.", idsList.Count, isActive);

            var statuses = await _repository.GetAllAsync();
            var statusesToUpdate = statuses.Where(s => idsList.Contains(s.Id)).ToList();

            foreach (var status in statusesToUpdate)
            {
                status.IsActive = isActive;
                await _repository.UpdateAsync(status);
            }

            _logger.LogInformation("وضعیت‌ها با موفقیت تغییر کردند.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در تغییر وضعیت گروهی وضعیت‌ها.");
            throw;
        }
    }
}