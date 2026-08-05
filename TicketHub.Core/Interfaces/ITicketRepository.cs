using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketHub.Core.Interfaces;

using TicketHub.Core.Entities;

public interface ITicketRepository : IRepository<Ticket>
{
    Task<(List<Ticket> Tickets, int TotalCount)> GetFilteredTicketsAsync(
            string? searchTerm, List<int>? projectIds, List<int>? statusIds, int? userId, int page, int pageSize);
}
