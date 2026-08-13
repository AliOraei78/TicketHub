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
            .Include(t => t.TicketHistories)
                .ThenInclude(th => th.Attachments)
            .Include(t => t.TicketHistories)
                .ThenInclude(th => th.TransitionFieldValues)
                    .ThenInclude(tfv => tfv.Attachments)
            .Include(t => t.Project)
                .ThenInclude(p => p.RoleProjects)
            .Include(t => t.Category)
            .Include(t => t.Status)
            .Include(t => t.Priority)
            .Include(t => t.WorkflowStatus)
            .Include(t => t.User)
            .AsSplitQuery()
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<(List<Ticket> Tickets, int TotalCount)> GetFilteredTicketsAsync(
        string? searchTerm, List<int>? projectIds, List<int>? statusIds, List<int>? priorityIds, int? userId,
        int currentUserId, List<int>? userRoleIds, bool isAdmin, bool isStaffOrAdmin, int page, int pageSize,
        string? sortBy = "createdAt", bool isAscending = false)
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
            .Include(t => t.WorkflowStatus)
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

        if (priorityIds != null && priorityIds.Any())
            query = query.Where(t => t.PriorityId.HasValue && priorityIds.Contains(t.PriorityId.Value));

        if (userId.HasValue) query = query.Where(t => t.UserId == userId.Value);

        var total = await query.CountAsync();

        IOrderedQueryable<Ticket> orderedQuery = (sortBy?.ToLower()) switch
        {
            "priority" => isAscending
                ? query.OrderBy(t => t.Priority != null ? t.Priority.Level : 0).ThenBy(t => t.CreatedAt)
                : query.OrderByDescending(t => t.Priority != null ? t.Priority.Level : 0).ThenByDescending(t => t.CreatedAt),

            "lastaction" => isAscending
                ? query.OrderBy(t => t.TicketHistories.Any() ? t.TicketHistories.Max(th => th.CreatedAt) : t.CreatedAt)
                : query.OrderByDescending(t => t.TicketHistories.Any() ? t.TicketHistories.Max(th => th.CreatedAt) : t.CreatedAt),

            _ => isAscending
                ? query.OrderBy(t => t.CreatedAt)
                : query.OrderByDescending(t => t.CreatedAt)
        };

        var tickets = await orderedQuery
                    .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return (tickets, total);
    }

    public async Task<(int Total, int NewCount, int InProgressCount, int ResolvedCount, int CriticalCount, int OverdueCount, int CriticalAndOverdueCount)> GetTicketTelemetryCountsAsync(
        string? searchTerm, List<int>? projectIds, List<int>? statusIds, List<int>? priorityIds, int? userId,
        int currentUserId, List<int>? userRoleIds, bool isAdmin, bool isStaffOrAdmin)
    {
        using var context = await _factory.CreateDbContextAsync();

        var query = context.Set<Ticket>().AsQueryable();

        // 1. Security Scoping: Admin / Staff / Customer
        if (!isAdmin)
        {
            if (!isStaffOrAdmin)
            {
                query = query.Where(t => t.UserId == currentUserId);
            }
            else
            {
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

        if (priorityIds != null && priorityIds.Any())
            query = query.Where(t => t.PriorityId.HasValue && priorityIds.Contains(t.PriorityId.Value));

        if (userId.HasValue) query = query.Where(t => t.UserId == userId.Value);

        var now = DateTime.UtcNow;

        var total = await query.CountAsync();

        var newCount = await query.CountAsync(t =>
            (t.WorkflowStatus != null && t.WorkflowStatus.IsInitial) ||
            (t.WorkflowStatusId == null && (t.Status.Name == "Open" || t.Status.Name == "جدید" || t.Status.Name == "اقدام نشده")));

        var resolvedCount = await query.CountAsync(t =>
            (t.WorkflowStatus != null && t.WorkflowStatus.IsFinal) ||
            (t.Status != null && (t.Status.Name == "Closed" || t.Status.Name == "Resolved" || t.Status.Name.Contains("بسته") || t.Status.Name.Contains("خاتمه") || t.Status.Name.Contains("حل"))));

        var inProgressCount = Math.Max(0, total - newCount - resolvedCount);

        var criticalCount = await query.CountAsync(t => t.Priority != null && t.Priority.Level >= 4);

        var overdueCount = await query.CountAsync(t =>
            t.DueDate != null && t.DueDate.Value <= now &&
            !(t.WorkflowStatus != null && t.WorkflowStatus.IsFinal) &&
            !(t.Status != null && (t.Status.Name == "Closed" || t.Status.Name == "Resolved" || t.Status.Name.Contains("بسته") || t.Status.Name.Contains("خاتمه") || t.Status.Name.Contains("حل"))));

        var criticalAndOverdueCount = await query.CountAsync(t =>
            (t.Priority != null && t.Priority.Level >= 4) ||
            (t.DueDate != null && t.DueDate.Value <= now &&
             !(t.WorkflowStatus != null && t.WorkflowStatus.IsFinal) &&
             !(t.Status != null && (t.Status.Name == "Closed" || t.Status.Name == "Resolved" || t.Status.Name.Contains("بسته") || t.Status.Name.Contains("خاتمه") || t.Status.Name.Contains("حل")))));

        return (total, newCount, inProgressCount, resolvedCount, criticalCount, overdueCount, criticalAndOverdueCount);
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
                    .ThenInclude(tfv => tfv.Attachments)
            .Include(t => t.TicketHistories)
                .ThenInclude(th => th.Attachments)
            .Include(t => t.Comments)
            .Include(t => t.Attachments)
            .AsSplitQuery()
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket != null)
        {
            var historyIds = ticket.TicketHistories?.Select(h => h.Id).ToList() ?? new List<int>();
            var ticketFieldValueIds = ticket.FieldValues?.Select(fv => fv.Id).ToList() ?? new List<int>();
            var transitionFieldValueIds = ticket.TicketHistories?
                .Where(th => th.TransitionFieldValues != null)
                .SelectMany(th => th.TransitionFieldValues.Select(tfv => tfv.Id))
                .ToList() ?? new List<int>();

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
                    if (th.TransitionFieldValues != null)
                    {
                        foreach (var tfv in th.TransitionFieldValues)
                        {
                            if (tfv.Attachments != null && tfv.Attachments.Any())
                            {
                                allAttachments.AddRange(tfv.Attachments);
                            }
                        }
                    }
                }
            }

            // Also query database for any attachments attached via FKs to this ticket or its children
            var dbAttachments = await context.Set<Attachment>()
                .Where(a => a.TicketId == id
                         || (a.TicketHistoryId != null && historyIds.Contains(a.TicketHistoryId.Value))
                         || (a.TicketFieldValueId != null && ticketFieldValueIds.Contains(a.TicketFieldValueId.Value))
                         || (a.TransitionFieldValueId != null && transitionFieldValueIds.Contains(a.TransitionFieldValueId.Value)))
                .ToListAsync();

            allAttachments.AddRange(dbAttachments);

            var distinctAttachments = allAttachments.DistinctBy(a => a.Id).ToList();
            if (distinctAttachments.Any())
            {
                context.Set<Attachment>().RemoveRange(distinctAttachments);
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

                var dbTransitionFieldValues = await context.Set<TransitionFieldValue>()
                    .Where(tfv => historyIds.Contains(tfv.TicketHistoryId))
                    .ToListAsync();
                if (dbTransitionFieldValues.Any())
                {
                    context.Set<TransitionFieldValue>().RemoveRange(dbTransitionFieldValues);
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