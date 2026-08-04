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

public class RoleService : IRoleService
{
    private readonly IRepository<Role> _repository;
    private readonly ILogger<RoleService> _logger;
    private readonly IValidator<RoleDto> _validator;

    public RoleService(
        IRepository<Role> repository,
        ILogger<RoleService> logger,
        IValidator<RoleDto> validator)
    {
        _repository = repository;
        _logger = logger;
        _validator = validator;
    }

    private async Task ValidateDtoAsync(RoleDto dto)
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

    public async Task<IEnumerable<RoleDto>> GetAllRolesAsync()
    {
        _logger.LogInformation("شروع دریافت لیست تمامی نقش‌ها.");
        var roles = await _repository.GetAllAsync();
        return roles.Adapt<IEnumerable<RoleDto>>();
    }

    public async Task CreateRoleAsync(RoleDto roleDto)
    {
        await ValidateDtoAsync(roleDto);

        _logger.LogInformation("شروع ایجاد نقش جدید.");
        var role = roleDto.Adapt<Role>();
        await _repository.AddAsync(role);
        _logger.LogInformation("نقش با موفقیت ایجاد شد.");
    }

    public async Task UpdateRoleAsync(int id, RoleDto roleDto)
    {
        await ValidateDtoAsync(roleDto);

        _logger.LogInformation("ویرایش نقش با شناسه {RoleId}.", id);

        var role = await _repository.GetByIdAsync(id);
        if (role == null)
            throw new NotFoundException("نقش", id);

        roleDto.Adapt(role);
        await _repository.UpdateAsync(role);
        _logger.LogInformation("نقش با شناسه {RoleId} با موفقیت ویرایش شد.", id);
    }

    public async Task DeleteRoleAsync(int id)
    {
        _logger.LogWarning("درخواست حذف نقش با شناسه {RoleId}.", id);

        var existingRole = await _repository.GetByIdAsync(id);
        if (existingRole == null)
            throw new NotFoundException("نقش", id);

        await _repository.DeleteAsync(id); // حذف قطعی از دیتابیس
        _logger.LogInformation("نقش با شناسه {RoleId} با موفقیت حذف شد.", id);
    }

    public async Task DeleteRolesAsync(IEnumerable<int> ids)
    {
        var idsList = ids.ToList();
        _logger.LogWarning("درخواست حذف گروهی نقش‌ها به تعداد {Count}.", idsList.Count);

        var roles = await _repository.GetAllAsync();
        var rolesToDelete = roles.Where(r => idsList.Contains(r.Id)).ToList();

        if (!rolesToDelete.Any())
            throw new NotFoundException("هیچ نقشی برای حذف یافت نشد.");

        await _repository.DeleteRangeAsync(rolesToDelete); // حذف قطعی گروهی
        _logger.LogInformation("تعداد {Count} نقش با موفقیت حذف شدند.", rolesToDelete.Count);
    }

    public async Task UpdateRolesStatusAsync(IEnumerable<int> ids, bool isActive)
    {
        var idsList = ids.ToList();
        _logger.LogInformation("درخواست تغییر وضعیت {Count} نقش به وضعیت فعال={IsActive}.", idsList.Count, isActive);

        var roles = await _repository.GetAllAsync();
        var rolesToUpdate = roles.Where(r => idsList.Contains(r.Id)).ToList();

        if (!rolesToUpdate.Any())
            throw new NotFoundException("هیچ نقشی برای تغییر وضعیت یافت نشد.");

        foreach (var role in rolesToUpdate)
        {
            role.IsActive = isActive;
            await _repository.UpdateAsync(role);
        }

        _logger.LogInformation("وضعیت نقش‌ها با موفقیت تغییر کرد.");
    }
}