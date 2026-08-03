namespace TicketHub.Application.Services;

using Mapster;
using TicketHub.Application.DTOs;
using TicketHub.Application.Interfaces;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;

public class TicketService : ITicketService
{
    private readonly ITicketRepository _ticketRepository;

    public TicketService(ITicketRepository ticketRepository)
    {
        _ticketRepository = ticketRepository;
    }

    public async Task<TicketDto?> GetByIdAsync(int id)
    {
        var ticket = await _ticketRepository.GetByIdAsync(id);
        return ticket?.Adapt<TicketDto>();
    }

    public async Task<(List<TicketDto> Tickets, int TotalCount)> GetFilteredTicketsAsync(
        string searchTerm, int? projectId, int? statusId, int? userId, int page, int pageSize)
    {
        var (tickets, totalCount) = await _ticketRepository.GetFilteredTicketsAsync(
            searchTerm, projectId, statusId, userId, page, pageSize);

        return (tickets.Adapt<List<TicketDto>>(), totalCount);
    }

    public async Task CreateAsync(TicketDto dto)
    {
        var ticket = dto.Adapt<Ticket>();
        ticket.CreatedAt = DateTime.UtcNow;

        await _ticketRepository.AddAsync(ticket);
    }

    public async Task UpdateAsync(TicketDto dto)
    {
        var ticketInDb = await _ticketRepository.GetByIdAsync(dto.Id);
        if (ticketInDb == null) return;

        dto.Adapt(ticketInDb);
        await _ticketRepository.UpdateAsync(ticketInDb);
    }

    public async Task DeleteAsync(int id)
    {
        await _ticketRepository.DeleteAsync(id);
    }
}