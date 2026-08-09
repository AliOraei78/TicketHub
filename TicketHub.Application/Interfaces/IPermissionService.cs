using TicketHub.Application.DTOs;

namespace TicketHub.Application.Interfaces;

public interface IPermissionService
{
    Task<IEnumerable<PermissionDto>> GetAllAsync();
    Task<PermissionDto?> GetByIdAsync(int id);
    Task<PermissionDto> CreateAsync(PermissionDto dto);
    Task<bool> UpdateAsync(PermissionDto dto);
    Task<bool> DeleteAsync(int id);
    Task<bool> AssignPermissionsToRoleAsync(int roleId, List<int> permissionIds);
    Task<bool> DeleteRangeAsync(IEnumerable<int> ids);
    Task<bool> UpdateStatusAsync(IEnumerable<int> ids, bool isActive);
    Task<bool> HasAccessAsync(System.Security.Claims.ClaimsPrincipal user, string resourceKey, TicketHub.Application.Enums.PermissionType minimumType = TicketHub.Application.Enums.PermissionType.Menu);
    void ClearCache();
}
