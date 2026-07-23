using TicketHub.Core.Entities;

public interface ITransitionService
{
    Task<IEnumerable<Transition>> GetAllAsync();
    Task CreateAsync(Transition entity);
    Task UpdateAsync(Transition entity);
    Task DeleteAsync(int id);
}