using Mapster;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;

namespace TicketHub.Application.Services;

public class PriorityService : IPriorityService
{
    private readonly IRepository<Priority> _repository;

    public PriorityService(IRepository<Priority> repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<PriorityDto>> GetAllAsync()
    {
        var data = await _repository.GetAllAsync();
        return data.Adapt<IEnumerable<PriorityDto>>();
    }

    public async Task AddAsync(PriorityDto priorityDto)
        => await _repository.AddAsync(priorityDto.Adapt<Priority>());

    public async Task UpdateAsync(PriorityDto priorityDto)
        => await _repository.UpdateAsync(priorityDto.Adapt<Priority>());

    public async Task DeleteAsync(int id) => await _repository.DeleteAsync(id);

    public async Task DeleteRangeAsync(IEnumerable<int> ids)
    {
        var items = (await _repository.GetAllAsync()).Where(p => ids.Contains(p.Id)).ToList();
        await _repository.DeleteRangeAsync(items);
    }

    public async Task UpdatePrioritiesStatusAsync(IEnumerable<int> ids, bool isActive)
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
