using Mapster;
using Microsoft.Extensions.Logging;
using FluentValidation;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Core.Common.Exceptions;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;
using ValidationException = TicketHub.Core.Common.Exceptions.ValidationException;

using Microsoft.AspNetCore.Http;
using TicketHub.Application.Enums;

namespace TicketHub.Application.Services;

public class PriorityService : IPriorityService
{
    private readonly IRepository<Priority> _repository;
    private readonly ILogger<PriorityService> _logger;
    private readonly IValidator<PriorityDto> _validator;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IPermissionService _permissionService;

    public PriorityService(
        IRepository<Priority> repository,
        ILogger<PriorityService> logger,
        IValidator<PriorityDto> validator,
        IHttpContextAccessor httpContextAccessor,
        IPermissionService permissionService)
    {
        _repository = repository;
        _logger = logger;
        _validator = validator;
        _httpContextAccessor = httpContextAccessor;
        _permissionService = permissionService;
    }

    private async Task EnsurePermissionAsync(PermissionType minType, string message)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user != null && user.Identity?.IsAuthenticated == true)
        {
            if (!await _permissionService.HasAccessAsync(user, "/settings/priorities", minType))
            {
                throw new ForbiddenException(message);
            }
        }
    }

    private async Task ValidateDtoAsync(PriorityDto dto)
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

    public async Task<IEnumerable<PriorityDto>> GetAllAsync()
    {
        _logger.LogInformation("شروع دریافت تمامی اولویت‌ها.");
        var data = await _repository.GetAllAsync();
        return data.Adapt<IEnumerable<PriorityDto>>();
    }

    public async Task AddAsync(PriorityDto priorityDto)
    {
        await EnsurePermissionAsync(PermissionType.SystemSection, "شما دسترسی لازم برای ایجاد اولویت را ندارید.");
        await ValidateDtoAsync(priorityDto);

        _logger.LogInformation("شروع ایجاد اولویت جدید.");
        await _repository.AddAsync(priorityDto.Adapt<Priority>());
        _logger.LogInformation("اولویت با موفقیت ایجاد شد.");
    }

    public async Task UpdateAsync(PriorityDto priorityDto)
    {
        await EnsurePermissionAsync(PermissionType.SystemSection, "شما دسترسی لازم برای ویرایش اولویت را ندارید.");
        await ValidateDtoAsync(priorityDto);

        _logger.LogInformation("ویرایش اولویت با شناسه {Id}.", priorityDto.Id);

        var existingEntity = await _repository.GetByIdAsync(priorityDto.Id);
        if (existingEntity == null)
            throw new NotFoundException("اولویت", priorityDto.Id);

        await _repository.UpdateAsync(priorityDto.Adapt<Priority>());
        _logger.LogInformation("اولویت با شناسه {Id} با موفقیت ویرایش شد.", priorityDto.Id);
    }

    public async Task DeleteAsync(int id)
    {
        await EnsurePermissionAsync(PermissionType.Full, "شما دسترسی لازم برای حذف اولویت را ندارید.");
        _logger.LogWarning("درخواست حذف اولویت با شناسه {Id}.", id);

        var existingEntity = await _repository.GetByIdAsync(id);
        if (existingEntity == null)
            throw new NotFoundException("اولویت", id);

        await _repository.DeleteAsync(id); // حذف قطعی از دیتابیس
        _logger.LogInformation("اولویت با شناسه {Id} با موفقیت حذف شد.", id);
    }

    public async Task DeleteRangeAsync(IEnumerable<int> ids)
    {
        await EnsurePermissionAsync(PermissionType.Full, "شما دسترسی لازم برای حذف گروهی اولویت‌ها را ندارید.");
        var idList = ids.ToList();
        _logger.LogWarning("درخواست حذف گروهی اولویت‌ها به تعداد {Count}.", idList.Count);

        var items = (await _repository.GetAllAsync()).Where(p => idList.Contains(p.Id)).ToList();

        if (!items.Any())
            throw new NotFoundException("هیچ اولویتی برای حذف یافت نشد.");

        await _repository.DeleteRangeAsync(items); // حذف قطعی از دیتابیس
        _logger.LogInformation("{Count} اولویت با موفقیت حذف شدند.", items.Count);
    }

    public async Task UpdatePrioritiesStatusAsync(IEnumerable<int> ids, bool isActive)
    {
        await EnsurePermissionAsync(PermissionType.SystemSection, "شما دسترسی لازم برای تغییر وضعیت اولویت‌ها را ندارید.");
        var idList = ids.ToList();
        _logger.LogInformation("درخواست تغییر وضعیت {Count} اولویت به وضعیت فعال={IsActive}.", idList.Count, isActive);

        var priorities = await _repository.GetAllAsync();
        var prioritiesToUpdate = priorities.Where(r => idList.Contains(r.Id)).ToList();

        if (!prioritiesToUpdate.Any())
            throw new NotFoundException("هیچ اولویتی برای تغییر وضعیت یافت نشد.");

        foreach (var priority in prioritiesToUpdate)
        {
            priority.IsActive = isActive;
            await _repository.UpdateAsync(priority);
        }

        _logger.LogInformation("وضعیت اولویت‌ها با موفقیت تغییر کرد.");
    }
}