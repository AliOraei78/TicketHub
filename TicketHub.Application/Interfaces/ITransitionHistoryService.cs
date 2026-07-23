using TicketHub.Core.Entities;

public interface ITransitionHistoryService
{
    Task<IEnumerable<TransitionHistory>> GetAllAsync();
    Task CreateAsync(TransitionHistory entity);
}