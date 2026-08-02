using TicketHub.Application.DTOs;

namespace TicketHub.Application.Interfaces;

public interface IPriorityService
{
    Task<IEnumerable<PriorityDto>> GetAllAsync();
    Task AddAsync(PriorityDto priorityDto);
    Task UpdateAsync(PriorityDto priorityDto);
    Task DeleteAsync(int id);
    Task DeleteRangeAsync(IEnumerable<int> ids);
    Task UpdatePrioritiesStatusAsync(IEnumerable<int> ids, bool isActive);
}
