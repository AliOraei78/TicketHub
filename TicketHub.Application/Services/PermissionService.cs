// TicketHub.Application.Services/PermissionService.cs
using Mapster;
using Microsoft.Extensions.Logging;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace TicketHub.Application.Services;

public class PermissionService : IPermissionService
{
    private readonly IRepository<Permission> _permissionRepo;
    private readonly IRepository<RolePermission> _rolePermissionRepo;
    private readonly IMemoryCache _cache;
    private const string CacheKey = "PermissionsCache";
    private static readonly System.Threading.SemaphoreSlim _semaphore = new(1, 1);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PermissionService> _logger;

    public PermissionService(
            IRepository<Permission> permissionRepo,
            IRepository<RolePermission> rolePermissionRepo,
            IMemoryCache cache,
            IServiceScopeFactory scopeFactory,
            ILogger<PermissionService> logger)
    {
        _permissionRepo = permissionRepo;
        _rolePermissionRepo = rolePermissionRepo;
        _cache = cache;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<IEnumerable<PermissionDto>> GetAllAsync()
    {
        try
        {
            _logger.LogInformation("شروع دریافت لیست تمامی دسترسی‌ها.");
            var permissions = await _permissionRepo.GetAllWithIncludesAsync(p => p.RolePermissions);
            return permissions.Adapt<IEnumerable<PermissionDto>>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت لیست دسترسی‌ها.");
            throw;
        }
    }

    public async Task<PermissionDto?> GetByIdAsync(int id)
    {
        try
        {
            _logger.LogInformation("جستجوی دسترسی با شناسه {Id}.", id);
            var permissions = await _permissionRepo.GetAllWithIncludesAsync(p => p.RolePermissions);
            var permission = permissions.FirstOrDefault(p => p.Id == id);
            return permission?.Adapt<PermissionDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت دسترسی با شناسه {Id}.", id);
            throw;
        }
    }

    public async Task<PermissionDto> CreateAsync(PermissionDto dto)
    {
        try
        {
            _logger.LogInformation("شروع ایجاد دسترسی جدید.");
            var permission = dto.Adapt<Permission>();

            if (dto.RoleIds != null && dto.RoleIds.Any())
            {
                permission.RolePermissions = dto.RoleIds.Select(roleId => new RolePermission
                {
                    RoleId = roleId
                }).ToList();
            }

            await _permissionRepo.AddAsync(permission);
            _logger.LogInformation("دسترسی جدید با شناسه {Id} با موفقیت ایجاد شد.", permission.Id);

            return permission.Adapt<PermissionDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در ایجاد دسترسی جدید.");
            throw;
        }
    }

    public async Task<bool> UpdateAsync(PermissionDto dto)
    {
        try
        {
            _logger.LogInformation("شروع ویرایش دسترسی با شناسه {Id}.", dto.Id);

            var permissions = await _permissionRepo.GetAllWithIncludesAsync(p => p.RolePermissions);
            var permission = permissions.FirstOrDefault(p => p.Id == dto.Id);

            if (permission == null)
            {
                _logger.LogWarning("دسترسی با شناسه {Id} جهت ویرایش یافت نشد.", dto.Id);
                return false;
            }

            dto.Adapt(permission);

            if (dto.RoleIds != null)
            {
                var existingRoles = permission.RolePermissions.ToList();
                if (existingRoles.Any())
                {
                    await _rolePermissionRepo.DeleteRangeAsync(existingRoles);
                }

                foreach (var roleId in dto.RoleIds)
                {
                    await _rolePermissionRepo.AddAsync(new RolePermission
                    {
                        PermissionId = permission.Id,
                        RoleId = roleId
                    });
                }
            }

            await _permissionRepo.UpdateAsync(permission);
            _logger.LogInformation("دسترسی با شناسه {Id} با موفقیت ویرایش شد.", dto.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در ویرایش دسترسی با شناسه {Id}.", dto.Id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            _logger.LogWarning("درخواست حذف دسترسی با شناسه {Id}.", id);

            var permissions = await _permissionRepo.GetAllWithIncludesAsync(p => p.RolePermissions);
            var permission = permissions.FirstOrDefault(p => p.Id == id);

            if (permission == null)
            {
                _logger.LogWarning("دسترسی با شناسه {Id} جهت حذف یافت نشد.", id);
                return false;
            }

            if (permission.RolePermissions != null && permission.RolePermissions.Any())
            {
                await _rolePermissionRepo.DeleteRangeAsync(permission.RolePermissions);
            }

            await _permissionRepo.DeleteAsync(id);
            _logger.LogInformation("دسترسی با شناسه {Id} با موفقیت حذف شد.", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در حذف دسترسی با شناسه {Id}.", id);
            throw;
        }
    }

    public async Task<bool> AssignPermissionsToRoleAsync(int roleId, List<int> permissionIds)
    {
        try
        {
            _logger.LogInformation("انتساب {Count} دسترسی به نقش با شناسه {RoleId}.", permissionIds.Count, roleId);

            var allRolePermissions = await _rolePermissionRepo.GetAllAsync();
            var existing = allRolePermissions.Where(rp => rp.RoleId == roleId).ToList();

            if (existing.Any())
            {
                await _rolePermissionRepo.DeleteRangeAsync(existing);
            }

            foreach (var pId in permissionIds)
            {
                await _rolePermissionRepo.AddAsync(new RolePermission
                {
                    RoleId = roleId,
                    PermissionId = pId
                });
            }

            _logger.LogInformation("دسترسی‌ها با موفقیت به نقش {RoleId} منتسب شدند.", roleId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در انتساب دسترسی‌ها به نقش با شناسه {RoleId}.", roleId);
            throw;
        }
    }

    public async Task<bool> DeleteRangeAsync(IEnumerable<int> ids)
    {
        try
        {
            var idsList = ids.ToList();
            _logger.LogWarning("درخواست حذف گروهی دسترسی‌ها به تعداد {Count}.", idsList.Count);

            var allPermissions = await _permissionRepo.GetAllWithIncludesAsync(p => p.RolePermissions);
            var targetPermissions = allPermissions.Where(p => idsList.Contains(p.Id)).ToList();

            if (!targetPermissions.Any())
                return false;

            var allRolePermissionsToDelete = targetPermissions
                .Where(p => p.RolePermissions != null)
                .SelectMany(p => p.RolePermissions)
                .ToList();

            if (allRolePermissionsToDelete.Any())
            {
                await _rolePermissionRepo.DeleteRangeAsync(allRolePermissionsToDelete);
            }

            await _permissionRepo.DeleteRangeAsync(targetPermissions);
            _logger.LogInformation("تعداد {Count} دسترسی با موفقیت حذف شدند.", targetPermissions.Count);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در حذف گروهی دسترسی‌ها.");
            throw;
        }
    }

    public async Task<bool> UpdateStatusAsync(IEnumerable<int> ids, bool isActive)
    {
        try
        {
            var idsList = ids.ToList();
            _logger.LogInformation("تغییر وضعیت {Count} دسترسی به فعال={IsActive}.", idsList.Count, isActive);

            var allPermissions = await _permissionRepo.GetAllAsync();
            var targetPermissions = allPermissions.Where(p => idsList.Contains(p.Id)).ToList();

            foreach (var permission in targetPermissions)
            {
                permission.IsActive = isActive;
                await _permissionRepo.UpdateAsync(permission);
            }

            _logger.LogInformation("وضعیت دسترسی‌ها با موفقیت تغییر کرد.");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در تغییر وضعیت گروهی دسترسی‌ها.");
            throw;
        }
    }

    public async Task<bool> HasAccessAsync(System.Security.Claims.ClaimsPrincipal user, string resourceKey)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(resourceKey)) return true;

            var permissions = await GetPermissionsFromCacheAsync();
            var targetPermission = permissions.FirstOrDefault(p =>
                p.ResourceKey.Equals(resourceKey, StringComparison.OrdinalIgnoreCase));

            if (targetPermission == null) return true;

            var userRoles = user.Claims
                .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();

            return targetPermission.AllowedRoles.Any(r => userRoles.Contains(r));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در بررسی دسترسی کاربر برای کلید {ResourceKey}.", resourceKey);
            throw;
        }
    }

    public void ClearCache()
    {
        try
        {
            _logger.LogInformation("پاکسازی کش دسترسی‌ها.");
            _cache.Remove(CacheKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در پاکسازی کش دسترسی‌ها.");
            throw;
        }
    }

    private async Task<List<PermissionCacheDto>> GetPermissionsFromCacheAsync()
    {
        if (_cache.TryGetValue(CacheKey, out List<PermissionCacheDto>? cached) && cached != null)
        {
            return cached;
        }

        await _semaphore.WaitAsync();
        try
        {
            _logger.LogInformation("بازیابی دسترسی‌ها از دیتابیس جهت بروزرسانی کش.");

            return await _cache.GetOrCreateAsync(CacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12);

                using var scope = _scopeFactory.CreateScope();

                var localPermRepo = scope.ServiceProvider.GetRequiredService<IRepository<Permission>>();
                var localRolePermRepo = scope.ServiceProvider.GetRequiredService<IRepository<RolePermission>>();

                var permissions = await localPermRepo.GetAllAsync();
                var rolePermissions = await localRolePermRepo.GetAllWithIncludesAsync(rp => rp.Role);

                return permissions
                    .Where(p => p.IsActive)
                    .Select(p => new PermissionCacheDto
                    {
                        ResourceKey = p.ResourceKey ?? string.Empty,
                        AllowedRoles = rolePermissions
                            .Where(rp => rp.PermissionId == p.Id && rp.Role != null)
                            .Select(rp => rp.Role.Name)
                            .ToList()
                    })
                    .ToList();
            }) ?? new List<PermissionCacheDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در بازیابی و کش کردن دسترسی‌ها.");
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}