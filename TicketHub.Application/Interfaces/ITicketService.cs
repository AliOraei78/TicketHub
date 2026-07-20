// TicketSystem.Application/Interfaces/ITicketService.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using TicketHub.Core.Entities;

namespace TicketSystem.Application.Interfaces
{
    public interface ITicketService
    {
        Task<Ticket?> GetTicketByIdAsync(int id);
        Task<IEnumerable<Ticket>> GetAllTicketsAsync();
        Task CreateTicketAsync(Ticket ticket);
        Task UpdateTicketAsync(Ticket ticket);
        Task DeleteTicketAsync(int id);
    }
}