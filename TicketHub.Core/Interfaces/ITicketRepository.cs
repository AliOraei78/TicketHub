using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketHub.Core.Interfaces;

using TicketHub.Core.Entities;

public interface ITicketRepository : IRepository<Ticket>
{
    new Task<Ticket?> GetByIdAsync(int id);
    Task<(List<Ticket> Tickets, int TotalCount)> GetFilteredTicketsAsync(
            string? searchTerm, List<int>? projectIds, List<int>? statusIds, int? userId, int page, int pageSize);
    Task<Ticket?> GetTicketWithProjectAndStatusAsync(int id);
    Task ApplyTransitionAndSaveHistoryAsync(int ticketId, int toStatusId, int? workflowStatusId, TicketHistory history);
}
