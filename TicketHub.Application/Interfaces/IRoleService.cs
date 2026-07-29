using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketHub.Application.DTOs;

namespace TicketHub.Application.Interfaces;

public interface IRoleService
{
    Task<IEnumerable<RoleDto>> GetAllRolesAsync();
    Task CreateRoleAsync(RoleDto roleDto);
    Task UpdateRoleAsync(int id, RoleDto roleDto);
    Task DeleteRoleAsync(int id);
    Task DeleteRolesAsync(IEnumerable<int> ids);
    Task UpdateRolesStatusAsync(IEnumerable<int> ids, bool isActive);
}
