using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;

public class TicketHistoryService : ITicketHistoryService
{
    private readonly IRepository<TicketHistory> _repo;
    public TicketHistoryService(IRepository<TicketHistory> repo) => _repo = repo;
    public async Task<IEnumerable<TicketHistory>> GetAllAsync() => await _repo.GetAllAsync();
    public async Task CreateAsync(TicketHistory entity) => await _repo.AddAsync(entity);
}