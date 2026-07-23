using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;

public class TransitionService : ITransitionService
{
    private readonly IRepository<Transition> _repo;
    public TransitionService(IRepository<Transition> repo) => _repo = repo;
    public async Task<IEnumerable<Transition>> GetAllAsync() => await _repo.GetAllAsync();
    public async Task CreateAsync(Transition entity) => await _repo.AddAsync(entity);
    public async Task UpdateAsync(Transition entity) => await _repo.UpdateAsync(entity);
    public async Task DeleteAsync(int id) => await _repo.DeleteAsync(id);
}