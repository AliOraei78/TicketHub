namespace TicketHub.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;
using TicketHub.Infrastructure.Data;

public class TicketRepository : GenericRepository<Ticket>, ITicketRepository
{
    public TicketRepository(AppDbContext context) : base(context) { }

    public async Task<(List<Ticket> Tickets, int TotalCount)> GetFilteredTicketsAsync(
        string searchTerm, int? projectId, int? statusId, int? userId, int page, int pageSize)
    {
        var query = _context.Set<Ticket>()
            .Include(t => t.Attachments)
            .Include(t => t.TicketHistories)
            .Include(t => t.Project)   // اضافه شد
            .Include(t => t.Status)    // اضافه شد
            .Include(t => t.Priority)  // اضافه شد
            .Include(t => t.User)      // اضافه شد
            .AsSplitQuery()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(t => t.Title.Contains(searchTerm) || t.Description.Contains(searchTerm));

        if (projectId.HasValue) query = query.Where(t => t.ProjectId == projectId.Value);
        if (statusId.HasValue) query = query.Where(t => t.StatusId == statusId.Value);
        if (userId.HasValue) query = query.Where(t => t.UserId == userId.Value);

        var total = await query.CountAsync();
        var tickets = await query.OrderByDescending(t => t.CreatedAt)
                    .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return (tickets, total);
    }
}