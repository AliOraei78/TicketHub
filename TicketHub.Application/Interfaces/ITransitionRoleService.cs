using TicketHub.Core.Entities;

public interface ITransitionRoleService
{
    Task<IEnumerable<TransitionRole>> GetAllAsync();
    Task CreateAsync(TransitionRole entity);
    Task DeleteAsync(int id);
}