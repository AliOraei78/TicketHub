using TicketHub.Application.DTOs;

namespace TicketHub.Application.Interfaces;

public interface ITicketFieldService
{
    Task<List<TicketFieldDto>> GetAllAsync();
    Task<TicketFieldDto?> GetByIdAsync(int id);
    Task<TicketFieldDto> AddAsync(TicketFieldDto dto);
    Task UpdateAsync(TicketFieldDto dto);
    Task DeleteAsync(TicketFieldDto dto);
    Task DeleteRangeAsync(IEnumerable<int> ids);
    Task UpdateTicketFieldsStatusAsync(IEnumerable<int> ids, bool isActive);
    Task<List<TicketFieldDto>> GetFieldsByCategoryIdAsync(int categoryId);
}
