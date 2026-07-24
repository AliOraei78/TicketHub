using TicketHub.Core.Entities;

public interface ITicketHistoryService
{
    Task<IEnumerable<TicketHistory>> GetAllAsync();
    Task CreateAsync(TicketHistory entity);
}