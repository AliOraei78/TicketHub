using Mapster;
using TicketHub.Application.DTOs;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;
using TicketHub.Application.Interfaces;

namespace TicketHub.Application.Services;

public class RoleService : IRoleService
{
    private readonly IRepository<Role> _repository;

    public RoleService(IRepository<Role> repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<RoleDto>> GetAllRolesAsync()
    {
        var roles = await _repository.GetAllAsync();
        return roles.Adapt<IEnumerable<RoleDto>>();
    }

    public async Task CreateRoleAsync(RoleDto roleDto)
    {
        var role = roleDto.Adapt<Role>();
        await _repository.AddAsync(role);
    }

    public async Task UpdateRoleAsync(int id, RoleDto roleDto)
    {
        var role = await _repository.GetByIdAsync(id);
        if (role != null)
        {
            roleDto.Adapt(role);
            await _repository.UpdateAsync(role);
        }
    }

    public async Task DeleteRoleAsync(int id)
    {
        await _repository.DeleteAsync(id);
    }

    public async Task DeleteRolesAsync(IEnumerable<int> ids)
    {
        var roles = await _repository.GetAllAsync();
        var rolesToDelete = roles.Where(r => ids.Contains(r.Id)).ToList();
        if (rolesToDelete.Any())
        {
            await _repository.DeleteRangeAsync(rolesToDelete);
        }
    }

    public async Task UpdateRolesStatusAsync(IEnumerable<int> ids, bool isActive)
    {
        var roles = await _repository.GetAllAsync();
        var rolesToUpdate = roles.Where(r => ids.Contains(r.Id)).ToList();

        foreach (var role in rolesToUpdate)
        {
            role.IsActive = isActive;
            await _repository.UpdateAsync(role);
        }
    }
}
