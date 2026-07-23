using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;

public class TransitionHistoryService : ITransitionHistoryService
{
    private readonly IRepository<TransitionHistory> _repo;
    public TransitionHistoryService(IRepository<TransitionHistory> repo) => _repo = repo;
    public async Task<IEnumerable<TransitionHistory>> GetAllAsync() => await _repo.GetAllAsync();
    public async Task CreateAsync(TransitionHistory entity) => await _repo.AddAsync(entity);
}