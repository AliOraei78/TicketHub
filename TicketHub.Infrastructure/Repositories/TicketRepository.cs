namespace TicketHub.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using TicketHub.Core.Entities;
using TicketHub.Core.Interfaces;
using TicketHub.Infrastructure.Data;

public class TicketRepository : GenericRepository<Ticket>, ITicketRepository
{
    public TicketRepository(IDbContextFactory<AppDbContext> factory) : base(factory) { }

    public new async Task<Ticket?> GetByIdAsync(int id)
    {
        using var context = await _factory.CreateDbContextAsync();

        return await context.Set<Ticket>()
            .Include(t => t.Attachments)
            .Include(t => t.FieldValues)
                .ThenInclude(fv => fv.Attachments)
            .Include(t => t.Project)
            .Include(t => t.Status)
            .Include(t => t.Priority)
            .Include(t => t.User)
            .AsSplitQuery()
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<(List<Ticket> Tickets, int TotalCount)> GetFilteredTicketsAsync(
        string? searchTerm, List<int>? projectIds, List<int>? statusIds, int? userId, int page, int pageSize)
    {
        using var context = await _factory.CreateDbContextAsync();

        var query = context.Set<Ticket>()
            .Include(t => t.Attachments)
            .Include(t => t.TicketHistories)
            .Include(t => t.Project)
            .Include(t => t.Status)
            .Include(t => t.Priority)
            .Include(t => t.User)
            .AsSplitQuery()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(t => t.Title.Contains(searchTerm) || t.Description.Contains(searchTerm));

        if (projectIds != null && projectIds.Any())
            query = query.Where(t => projectIds.Contains(t.ProjectId));

        if (statusIds != null && statusIds.Any())
            query = query.Where(t => statusIds.Contains(t.StatusId));

        if (userId.HasValue) query = query.Where(t => t.UserId == userId.Value);

        var total = await query.CountAsync();
        var tickets = await query.OrderByDescending(t => t.CreatedAt)
                    .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return (tickets, total);
    }
}