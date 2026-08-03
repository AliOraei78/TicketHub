// TicketHub.Application.Services/PermissionService.cs
using Mapster;
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

    public PermissionService(
            IRepository<Permission> permissionRepo,
            IRepository<RolePermission> rolePermissionRepo,
            IMemoryCache cache,
            IServiceScopeFactory scopeFactory) // <--- این خط اضافه شد
    {
        _permissionRepo = permissionRepo;
        _rolePermissionRepo = rolePermissionRepo;
        _cache = cache;
        _scopeFactory = scopeFactory; // <--- این خط اضافه شد
    }

    public async Task<IEnumerable<PermissionDto>> GetAllAsync()
    {
        var permissions = await _permissionRepo.GetAllWithIncludesAsync(p => p.RolePermissions);
        return permissions.Adapt<IEnumerable<PermissionDto>>();
    }

    public async Task<PermissionDto?> GetByIdAsync(int id)
    {
        var permissions = await _permissionRepo.GetAllWithIncludesAsync(p => p.RolePermissions);
        var permission = permissions.FirstOrDefault(p => p.Id == id);
        return permission?.Adapt<PermissionDto>();
    }

    public async Task<PermissionDto> CreateAsync(PermissionDto dto)
    {
        var permission = dto.Adapt<Permission>();

        // اضافه کردن نقش‌های انتخاب شده
        if (dto.RoleIds != null && dto.RoleIds.Any())
        {
            permission.RolePermissions = dto.RoleIds.Select(roleId => new RolePermission
            {
                RoleId = roleId
            }).ToList();
        }

        await _permissionRepo.AddAsync(permission);
        return permission.Adapt<PermissionDto>();
    }

    public async Task<bool> UpdateAsync(PermissionDto dto)
    {
        // لود کردن دسترسی به همراه نقش‌های قبلی برای آپدیت صحیح
        var permissions = await _permissionRepo.GetAllWithIncludesAsync(p => p.RolePermissions);
        var permission = permissions.FirstOrDefault(p => p.Id == dto.Id);

        if (permission == null) return false;

        dto.Adapt(permission);

        // بروزرسانی نقش‌ها
        if (dto.RoleIds != null)
        {
            // حذف نقش‌های قبلی
            var existingRoles = permission.RolePermissions.ToList();
            if (existingRoles.Any())
            {
                await _rolePermissionRepo.DeleteRangeAsync(existingRoles);
            }

            // اضافه کردن نقش‌های جدید
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
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        // 1. پیدا کردن دسترسی به همراه نقش‌های متصل
        var permissions = await _permissionRepo.GetAllWithIncludesAsync(p => p.RolePermissions);
        var permission = permissions.FirstOrDefault(p => p.Id == id);

        if (permission == null) return false;

        // 2. حذف رکوردهای واسط (RolePermissions) متصل به این دسترسی
        if (permission.RolePermissions != null && permission.RolePermissions.Any())
        {
            await _rolePermissionRepo.DeleteRangeAsync(permission.RolePermissions);
        }

        // 3. حذف خود دسترسی
        await _permissionRepo.DeleteAsync(id);
        return true;
    }

    public async Task<bool> AssignPermissionsToRoleAsync(int roleId, List<int> permissionIds)
    {
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
        return true;
    }

    public async Task<bool> DeleteRangeAsync(IEnumerable<int> ids)
    {
        // 1. پیدا کردن دسترسی‌ها به همراه نقش‌های متصل
        var allPermissions = await _permissionRepo.GetAllWithIncludesAsync(p => p.RolePermissions);
        var targetPermissions = allPermissions.Where(p => ids.Contains(p.Id)).ToList();

        if (!targetPermissions.Any()) return false;

        // 2. استخراج و حذف تمام رکوردهای واسط (RolePermissions) متصل به این دسترسی‌ها
        var allRolePermissionsToDelete = targetPermissions
            .Where(p => p.RolePermissions != null)
            .SelectMany(p => p.RolePermissions)
            .ToList();

        if (allRolePermissionsToDelete.Any())
        {
            await _rolePermissionRepo.DeleteRangeAsync(allRolePermissionsToDelete);
        }

        // 3. حذف گروهی دسترسی‌ها
        await _permissionRepo.DeleteRangeAsync(targetPermissions);

        return true;
    }

    public async Task<bool> UpdateStatusAsync(IEnumerable<int> ids, bool isActive)
    {
        var allPermissions = await _permissionRepo.GetAllAsync();
        var targetPermissions = allPermissions.Where(p => ids.Contains(p.Id)).ToList();

        foreach (var permission in targetPermissions)
        {
            permission.IsActive = isActive;
            await _permissionRepo.UpdateAsync(permission);
        }
        return true;
    }

    public async Task<bool> HasAccessAsync(System.Security.Claims.ClaimsPrincipal user, string resourceKey)
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

    public void ClearCache()
    {
        _cache.Remove(CacheKey);
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
            return await _cache.GetOrCreateAsync(CacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12);

                // ایجاد یک ناحیه (Scope) جدید برای جلوگیری از تداخل DbContext با سایر کامپوننت‌ها (مثل Home.razor)
                using var scope = _scopeFactory.CreateScope();

                // دریافت نمونه‌های کاملاً جدید و مستقل از دیتابیس
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
        finally
        {
            _semaphore.Release();
        }
    }
}