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
            string? searchTerm, List<int>? projectIds, List<int>? statusIds, List<int>? priorityIds, int? userId,
            int currentUserId, List<int>? userRoleIds, bool isAdmin, bool isStaffOrAdmin, int page, int pageSize);
    Task<(int Total, int NewCount, int InProgressCount, int ResolvedCount, int CriticalCount, int OverdueCount, int CriticalAndOverdueCount)> GetTicketTelemetryCountsAsync(
            string? searchTerm, List<int>? projectIds, List<int>? statusIds, List<int>? priorityIds, int? userId,
            int currentUserId, List<int>? userRoleIds, bool isAdmin, bool isStaffOrAdmin);
    Task<Ticket?> GetTicketWithProjectAndStatusAsync(int id);
    Task ApplyTransitionAndSaveHistoryAsync(int ticketId, int toStatusId, int? workflowStatusId, TicketHistory history);
}
