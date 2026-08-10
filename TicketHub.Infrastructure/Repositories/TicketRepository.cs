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
                .ThenInclude(p => p.RoleProjects)
            .Include(t => t.Category)
            .Include(t => t.Status)
            .Include(t => t.Priority)
            .Include(t => t.User)
            .AsSplitQuery()
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<(List<Ticket> Tickets, int TotalCount)> GetFilteredTicketsAsync(
        string? searchTerm, List<int>? projectIds, List<int>? statusIds, int? userId,
        int currentUserId, List<int>? userRoleIds, bool isAdmin, bool isStaffOrAdmin, int page, int pageSize)
    {
        using var context = await _factory.CreateDbContextAsync();

        var query = context.Set<Ticket>()
            .Include(t => t.Attachments)
            .Include(t => t.TicketHistories)
            .Include(t => t.Project)
                .ThenInclude(p => p.RoleProjects)
            .Include(t => t.Category)
            .Include(t => t.Status)
            .Include(t => t.Priority)
            .Include(t => t.User)
            .AsSplitQuery()
            .AsQueryable();

        // 1. Security Scoping: Admin / Staff / Customer
        if (!isAdmin)
        {
            if (!isStaffOrAdmin)
            {
                // Customer Mode (Menu level): strictly own tickets
                query = query.Where(t => t.UserId == currentUserId);
            }
            else
            {
                // Staff / Agent Mode (SystemSection/Full level): own tickets + tickets in assigned project roles
                var validRoleIds = userRoleIds ?? new List<int>();
                query = query.Where(t =>
                    t.UserId == currentUserId ||
                    !t.Project.RoleProjects.Any() ||
                    t.Project.RoleProjects.Any(rp => validRoleIds.Contains(rp.RoleId))
                );
            }
        }

        // 2. User-Selected Filters
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

    public async Task<Ticket?> GetTicketWithProjectAndStatusAsync(int id)
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.Set<Ticket>()
            .Include(t => t.Project)
                .ThenInclude(p => p.Workflow)
            .Include(t => t.Status)
            .Include(t => t.WorkflowStatus)
                .ThenInclude(ws => ws.Status)
            .AsSplitQuery()
            .FirstOrDefaultAsync(t => t.Id == id);
    }


    public async Task ApplyTransitionAndSaveHistoryAsync(int ticketId, int toStatusId, int? workflowStatusId, TicketHistory history)
    {
        using var context = await _factory.CreateDbContextAsync();
        
        var ticket = await context.Set<Ticket>().FirstOrDefaultAsync(t => t.Id == ticketId);
        if (ticket != null)
        {
            ticket.StatusId = toStatusId;
            if (workflowStatusId.HasValue)
            {
                ticket.WorkflowStatusId = workflowStatusId.Value;
            }
            
            context.Set<TicketHistory>().Add(history);
            await context.SaveChangesAsync();
        }
    }

    public override async Task DeleteAsync(int id)
    {
        using var context = await _factory.CreateDbContextAsync();

        var ticket = await context.Set<Ticket>()
            .Include(t => t.FieldValues)
                .ThenInclude(fv => fv.Attachments)
            .Include(t => t.TicketHistories)
                .ThenInclude(th => th.TransitionFieldValues)
            .Include(t => t.TicketHistories)
                .ThenInclude(th => th.Attachments)
            .Include(t => t.Comments)
            .Include(t => t.Attachments)
            .AsSplitQuery()
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket != null)
        {
            var allAttachments = new List<Attachment>();

            if (ticket.Attachments != null && ticket.Attachments.Any())
            {
                allAttachments.AddRange(ticket.Attachments);
            }

            if (ticket.FieldValues != null)
            {
                foreach (var fv in ticket.FieldValues)
                {
                    if (fv.Attachments != null && fv.Attachments.Any())
                    {
                        allAttachments.AddRange(fv.Attachments);
                    }
                }
            }

            if (ticket.TicketHistories != null)
            {
                foreach (var th in ticket.TicketHistories)
                {
                    if (th.Attachments != null && th.Attachments.Any())
                    {
                        allAttachments.AddRange(th.Attachments);
                    }
                }
            }

            if (allAttachments.Any())
            {
                context.Set<Attachment>().RemoveRange(allAttachments.Distinct());
            }

            if (ticket.TicketHistories != null && ticket.TicketHistories.Any())
            {
                foreach (var history in ticket.TicketHistories)
                {
                    if (history.TransitionFieldValues != null && history.TransitionFieldValues.Any())
                    {
                        context.Set<TransitionFieldValue>().RemoveRange(history.TransitionFieldValues);
                    }
                }
                context.Set<TicketHistory>().RemoveRange(ticket.TicketHistories);
            }

            if (ticket.FieldValues != null && ticket.FieldValues.Any())
            {
                context.Set<TicketFieldValue>().RemoveRange(ticket.FieldValues);
            }

            if (ticket.Comments != null && ticket.Comments.Any())
            {
                context.Set<Comment>().RemoveRange(ticket.Comments);
            }

            context.Set<Ticket>().Remove(ticket);
            await context.SaveChangesAsync();
        }
    }
}