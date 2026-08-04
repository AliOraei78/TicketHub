using Mapster;
using Microsoft.Extensions.Logging;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;

namespace TicketHub.Application.Services;

public class PriorityService : IPriorityService
{
    private readonly IRepository<Priority> _repository;
    private readonly ILogger<PriorityService> _logger;

    public PriorityService(
        IRepository<Priority> repository,
        ILogger<PriorityService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IEnumerable<PriorityDto>> GetAllAsync()
    {
        try
        {
            _logger.LogInformation("شروع دریافت تمامی اولویت‌ها.");
            var data = await _repository.GetAllAsync();
            return data.Adapt<IEnumerable<PriorityDto>>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت تمامی اولویت‌ها.");
            throw;
        }
    }

    public async Task AddAsync(PriorityDto priorityDto)
    {
        try
        {
            _logger.LogInformation("شروع ایجاد اولویت جدید.");
            await _repository.AddAsync(priorityDto.Adapt<Priority>());
            _logger.LogInformation("اولویت با موفقیت ایجاد شد.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در ایجاد اولویت جدید.");
            throw;
        }
    }

    public async Task UpdateAsync(PriorityDto priorityDto)
    {
        try
        {
            _logger.LogInformation("ویرایش اولویت با شناسه {Id}.", priorityDto.Id);
            await _repository.UpdateAsync(priorityDto.Adapt<Priority>());
            _logger.LogInformation("اولویت با شناسه {Id} با موفقیت ویرایش شد.", priorityDto.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در ویرایش اولویت با شناسه {Id}.", priorityDto.Id);
            throw;
        }
    }

    public async Task DeleteAsync(int id)
    {
        try
        {
            _logger.LogWarning("درخواست حذف اولویت با شناسه {Id}.", id);
            await _repository.DeleteAsync(id);
            _logger.LogInformation("اولویت با شناسه {Id} با موفقیت حذف شد.", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در حذف اولویت با شناسه {Id}.", id);
            throw;
        }
    }

    public async Task DeleteRangeAsync(IEnumerable<int> ids)
    {
        try
        {
            var idList = ids.ToList();
            _logger.LogWarning("درخواست حذف گروهی اولویت‌ها به تعداد {Count}.", idList.Count);

            var items = (await _repository.GetAllAsync()).Where(p => idList.Contains(p.Id)).ToList();

            if (items.Any())
            {
                await _repository.DeleteRangeAsync(items);
                _logger.LogInformation("{Count} اولویت با موفقیت حذف شدند.", items.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در حذف گروهی اولویت‌ها.");
            throw;
        }
    }

    public async Task UpdatePrioritiesStatusAsync(IEnumerable<int> ids, bool isActive)
    {
        try
        {
            var idList = ids.ToList();
            _logger.LogInformation("درخواست تغییر وضعیت {Count} اولویت به وضعیت فعال={IsActive}.", idList.Count, isActive);

            var priorities = await _repository.GetAllAsync();
            var prioritiesToUpdate = priorities.Where(r => idList.Contains(r.Id)).ToList();

            foreach (var priority in prioritiesToUpdate)
            {
                priority.IsActive = isActive;
                await _repository.UpdateAsync(priority);
            }

            _logger.LogInformation("وضعیت اولویت‌ها با موفقیت تغییر کرد.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در تغییر وضعیت گروهی اولویت‌ها.");
            throw;
        }
    }
}