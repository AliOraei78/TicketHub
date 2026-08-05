using TicketHub.Application.DTOs;

namespace TicketHub.Application.Interfaces;

public interface ITicketService
{
    Task<TicketDto?> GetByIdAsync(int id);
    Task<(List<TicketDto> Tickets, int TotalCount)> GetFilteredTicketsAsync(string? searchTerm, 
        List<int>? projectIds, List<int>? statusIds, 
        int? userId, int page, int pageSize);
    Task CreateAsync(TicketDto dto);
    Task UpdateAsync(TicketDto dto);
    Task DeleteAsync(int id);
}
