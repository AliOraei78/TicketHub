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

    public async Task<IEnumerable<Priority>> GetAllAsync() => await _repository.GetAllAsync();

    public async Task AddAsync(Priority priority) => await _repository.AddAsync(priority);

    public async Task UpdateAsync(Priority priority) => await _repository.UpdateAsync(priority);

    public async Task DeleteAsync(int id) => await _repository.DeleteAsync(id);

    public async Task DeleteRangeAsync(IEnumerable<int> ids)
    {
        var items = (await _repository.GetAllAsync()).Where(p => ids.Contains(p.Id)).ToList();
        await _repository.DeleteRangeAsync(items);
    }
}
