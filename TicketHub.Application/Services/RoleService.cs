using Mapster;
using Microsoft.Extensions.Logging;
using TicketHub.Application.DTOs;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;
using TicketHub.Application.Interfaces;

namespace TicketHub.Application.Services;

public class RoleService : IRoleService
{
    private readonly IRepository<Role> _repository;
    private readonly ILogger<RoleService> _logger;

    public RoleService(
        IRepository<Role> repository,
        ILogger<RoleService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IEnumerable<RoleDto>> GetAllRolesAsync()
    {
        try
        {
            _logger.LogInformation("شروع دریافت لیست تمامی نقش‌ها.");
            var roles = await _repository.GetAllAsync();
            return roles.Adapt<IEnumerable<RoleDto>>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت لیست تمامی نقش‌ها.");
            throw;
        }
    }

    public async Task CreateRoleAsync(RoleDto roleDto)
    {
        try
        {
            _logger.LogInformation("شروع ایجاد نقش جدید.");
            var role = roleDto.Adapt<Role>();
            await _repository.AddAsync(role);
            _logger.LogInformation("نقش با موفقیت ایجاد شد.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در ایجاد نقش جدید.");
            throw;
        }
    }

    public async Task UpdateRoleAsync(int id, RoleDto roleDto)
    {
        try
        {
            _logger.LogInformation("ویرایش نقش با شناسه {RoleId}.", id);

            var role = await _repository.GetByIdAsync(id);
            if (role != null)
            {
                roleDto.Adapt(role);
                await _repository.UpdateAsync(role);
                _logger.LogInformation("نقش با شناسه {RoleId} با موفقیت ویرایش شد.", id);
            }
            else
            {
                _logger.LogWarning("نقش با شناسه {RoleId} جهت ویرایش یافت نشد.", id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در ویرایش نقش با شناسه {RoleId}.", id);
            throw;
        }
    }

    public async Task DeleteRoleAsync(int id)
    {
        try
        {
            _logger.LogWarning("درخواست حذف نقش با شناسه {RoleId}.", id);
            await _repository.DeleteAsync(id);
            _logger.LogInformation("نقش با شناسه {RoleId} با موفقیت حذف شد.", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در حذف نقش با شناسه {RoleId}.", id);
            throw;
        }
    }

    public async Task DeleteRolesAsync(IEnumerable<int> ids)
    {
        try
        {
            var idsList = ids.ToList();
            _logger.LogWarning("درخواست حذف گروهی نقش‌ها به تعداد {Count}.", idsList.Count);

            var roles = await _repository.GetAllAsync();
            var rolesToDelete = roles.Where(r => idsList.Contains(r.Id)).ToList();

            if (rolesToDelete.Any())
            {
                await _repository.DeleteRangeAsync(rolesToDelete);
                _logger.LogInformation("تعداد {Count} نقش با موفقیت حذف شدند.", rolesToDelete.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در حذف گروهی نقش‌ها.");
            throw;
        }
    }

    public async Task UpdateRolesStatusAsync(IEnumerable<int> ids, bool isActive)
    {
        try
        {
            var idsList = ids.ToList();
            _logger.LogInformation("درخواست تغییر وضعیت {Count} نقش به وضعیت فعال={IsActive}.", idsList.Count, isActive);

            var roles = await _repository.GetAllAsync();
            var rolesToUpdate = roles.Where(r => idsList.Contains(r.Id)).ToList();

            foreach (var role in rolesToUpdate)
            {
                role.IsActive = isActive;
                await _repository.UpdateAsync(role);
            }

            _logger.LogInformation("وضعیت نقش‌ها با موفقیت تغییر کرد.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در تغییر وضعیت گروهی نقش‌ها.");
            throw;
        }
    }
}