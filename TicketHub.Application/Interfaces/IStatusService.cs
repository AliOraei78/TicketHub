using TicketHub.Application.DTOs;

namespace TicketHub.Application.Interfaces;

public interface IStatusService
{
    Task<List<StatusDto>> GetAllAsync();
    Task AddAsync(StatusDto dto);
    Task UpdateAsync(StatusDto dto);
    Task DeleteAsync(int id);
    Task DeleteRangeAsync(IEnumerable<int> ids);
}
