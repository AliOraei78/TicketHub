// TicketSystem.Application/Services/TicketService.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;
using TicketSystem.Application.Interfaces;

namespace TicketSystem.Application.Services
{
    public class TicketService : ITicketService
    {
        private readonly IRepository<Ticket> _ticketRepository;

        public TicketService(IRepository<Ticket> ticketRepository)
        {
            _ticketRepository = ticketRepository;
        }

        public async Task<Ticket?> GetTicketByIdAsync(int id)
        {
            return await _ticketRepository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<Ticket>> GetAllTicketsAsync()
        {
            // Fetching tickets along with their Project and User relationships
            return await _ticketRepository.GetAllWithIncludesAsync(t => t.Project, t => t.User);
        }

        public async Task CreateTicketAsync(Ticket ticket)
        {
            await _ticketRepository.AddAsync(ticket);
        }

        public async Task UpdateTicketAsync(Ticket ticket)
        {
            await _ticketRepository.UpdateAsync(ticket);
        }

        public async Task DeleteTicketAsync(int id)
        {
            await _ticketRepository.DeleteAsync(id);
        }
    }
}