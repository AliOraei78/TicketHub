using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;

public class TransitionRoleService : ITransitionRoleService
{
    private readonly IRepository<TransitionRole> _repo;
    public TransitionRoleService(IRepository<TransitionRole> repo) => _repo = repo;
    public async Task<IEnumerable<TransitionRole>> GetAllAsync() => await _repo.GetAllAsync();
    public async Task CreateAsync(TransitionRole entity) => await _repo.AddAsync(entity);
    public async Task DeleteAsync(int id) => await _repo.DeleteAsync(id);
}